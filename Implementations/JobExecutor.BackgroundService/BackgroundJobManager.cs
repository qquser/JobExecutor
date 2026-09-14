using System.Threading.Channels;
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Options;
using JobExecutor.Abstractions.Models.Queries;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobExecutor.BackgroundService;

internal sealed class BackgroundJobManager<TIn, TOut>(
                        Channel<StartJobRequest<TIn>> startChannel,
                        Channel<RunJobRequest<TIn>> runChannel,
                        IJobEntryFactory<TIn, TOut> entryFactory,
                        IJobRegistry<TIn, TOut> registry,
                        ILogger<BackgroundJobManager<TIn, TOut>> logger,
                        IOptions<JobTimeoutOptions> timeoutOptions)

    : IActiveJobManager<TIn, TOut>

        where TIn : class
        where TOut : class
{
    public async Task<JobStartedResult> StartJobAsync(JobId jobId, TIn input)
    {
        if (string.IsNullOrWhiteSpace(jobId.Value))
            return new JobStartedResult(false, "Job id is required.");

        var request = entryFactory.CreateStartRequest(jobId, input);
        await startChannel.Writer.WriteAsync(request);

        try
        {
            return await request.Started.Task.WaitAsync(timeoutOptions.Value.Timeout);
        }
        catch (TimeoutException)
        {
            await request.Cts.CancelAsync();
            logger.LogWarning("Job {JobId} did not start within the timeout.", request.JobId.Value);
            return new JobStartedResult(false, "Timeout.");
        }
    }

    public async Task<JobCompletedResult> RunJobAsync(JobId jobId, TIn input)
    {
        if (string.IsNullOrWhiteSpace(jobId.Value))
            return new JobCompletedResult(false, "Job id is required.");

        var request = entryFactory.CreateRunRequest(jobId, input);
        await runChannel.Writer.WriteAsync(request);

        try
        {
            return await request.Completed.Task.WaitAsync(timeoutOptions.Value.Timeout);
        }
        catch (TimeoutException)
        {
            await request.Cts.CancelAsync();
            logger.LogWarning("Job {JobId} did not complete within the timeout.", request.JobId.Value);
            return new JobCompletedResult(false, "Timeout.");
        }
    }

    public Task<JobStoppedResult> StopJobAsync(JobId jobId)
    {
        if (registry.TryGet(jobId, out var registered))
        {
            registered.Cts.Cancel();
            return Task.FromResult(new JobStoppedResult(true, string.Empty));
        }

        return Task.FromResult(new JobStoppedResult(false, $"Job list does not contain {jobId.Value}"));
    }

    public Task<ActiveJobsQueryResult<TOut>> GetAllJobsAsync()
        => Task.FromResult(registry.GetAll());

    public Task<ActiveJobsQueryResult<TOut>> GetJobsPageAsync(int skip, int take)
        => Task.FromResult(registry.GetPage(skip, take));

    public Task<ActiveJobsQueryResult<TOut>> GetJobsByIdsAsync(ICollection<JobId> jobIds)
        => Task.FromResult(registry.GetByIds(jobIds));
}
