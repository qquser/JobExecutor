using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Queries;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using Microsoft.Extensions.Logging;

namespace JobExecutor.BackgroundService;

[SuppressMessage(
    "ReSharper",
    "ConditionIsAlwaysTrueOrFalseAccordingToNullableApiContract",
    Justification = "The identifier comes from the caller and can be missing.")]
internal sealed class BackgroundJobManager<TIn, TOut>(
                        Channel<StartJobRequest<TIn>> startChannel,
                        Channel<RunJobRequest<TIn>> runChannel,
                        IJobEntryFactory<TIn> entryFactory,
                        IJobRegistry<TIn, TOut> registry,
                        ILogger<BackgroundJobManager<TIn, TOut>> logger,
                        IJobTimeouts<TIn, TOut> timeouts)

    : IActiveJobManager<TIn, TOut>

        where TIn : class
        where TOut : class
{
    public async Task<JobStartedResult> StartJobAsync(JobId jobId, TIn input, CancellationToken token = default)
    {
        if (jobId is null || string.IsNullOrWhiteSpace(jobId.Value))
            return new JobStartedResult(false, "Job id is required.");

        if (token.IsCancellationRequested)
            return new JobStartedResult(false, "cancelled");

        var request = entryFactory.CreateStartRequest(jobId, input, token);
        await startChannel.Writer.WriteAsync(request, token);

        try
        {
            return await request.Started.Task.WaitAsync(timeouts.Value.StartTimeout, token);
        }
        catch (TimeoutException)
        {
            await CancelAsync(request.StopSource, request.JobId);
            logger.LogWarning(
                "Job {JobId} did not start within the start timeout of {StartTimeout}.",
                request.JobId.Value,
                timeouts.Value.StartTimeout);
            return new JobStartedResult(false, "Start timeout.");
        }
    }

    public async Task<JobCompletedResult> RunJobAsync(JobId jobId, TIn input, CancellationToken token = default)
    {
        if (jobId is null || string.IsNullOrWhiteSpace(jobId.Value))
            return new JobCompletedResult(false, "Job id is required.");

        if (token.IsCancellationRequested)
            return new JobCompletedResult(false, "cancelled");

        var request = entryFactory.CreateRunRequest(jobId, input, token);
        await runChannel.Writer.WriteAsync(request, token);

        JobStartedResult started;
        try
        {
            started = await request.Started.Task.WaitAsync(timeouts.Value.StartTimeout, token);
        }
        catch (TimeoutException)
        {
            await CancelAsync(request.StopSource, request.JobId);
            logger.LogWarning(
                "Job {JobId} did not start within the start timeout of {StartTimeout}.",
                request.JobId.Value,
                timeouts.Value.StartTimeout);
            return new JobCompletedResult(false, "Start timeout.");
        }

        if (!started.Success)
            return new JobCompletedResult(false, started.Result);

        try
        {
            return await request.Completed.Task.WaitAsync(timeouts.Value.ExecutionTimeout, token);
        }
        catch (TimeoutException)
        {
            await CancelAsync(request.StopSource, request.JobId);
            logger.LogWarning(
                "Job {JobId} did not complete within the execution timeout of {ExecutionTimeout}.",
                request.JobId.Value,
                timeouts.Value.ExecutionTimeout);
            return new JobCompletedResult(false, "Execution timeout.");
        }
    }

    public async Task<JobStoppedResult> StopJobAsync(JobId jobId)
    {
        if (jobId is null || string.IsNullOrWhiteSpace(jobId.Value))
            return new JobStoppedResult(false, "Job id is required.");

        if (registry.TryGet(jobId, out var registered))
        {
            await CancelAsync(registered.StopSource, jobId);
            return new JobStoppedResult(true, string.Empty);
        }

        return new JobStoppedResult(false, $"Job list does not contain {jobId.Value}");
    }

    public Task<ActiveJobsQueryResult<TOut>> GetAllJobsAsync()
        => Task.FromResult(registry.GetAll());

    public Task<ActiveJobsQueryResult<TOut>> GetJobsPageAsync(int skip, int take)
        => Task.FromResult(registry.GetPage(skip, take));

    public Task<ActiveJobsQueryResult<TOut>> GetJobsByIdsAsync(ICollection<JobId> jobIds)
        => Task.FromResult(registry.GetByIds(jobIds));

    /// <summary>
    /// The cancellation of a job must not escape the manager: a job that registers a throwing
    /// callback on its token makes the cancellation fail with an aggregate exception, and the
    /// caller must still receive the result of the command.
    /// </summary>
    private async Task CancelAsync(CancellationTokenSource source, JobId jobId)
    {
        try
        {
            await source.CancelAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "The cancellation of the job {JobId} failed.", jobId.Value);
        }
    }
}
