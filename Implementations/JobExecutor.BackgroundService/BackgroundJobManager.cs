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
                        Channel<JobRequest<TIn>> channel,
                        IJobEntryFactory<TIn, TOut> entryFactory,
                        IJobRegistry<TIn, TOut> registry,
                        IJobStateMapper<TIn, TOut> stateMapper,
                        ILogger<BackgroundJobManager<TIn, TOut>> logger,
                        IOptions<JobTimeoutOptions> timeoutOptions)

    : IActiveJobManager<TIn, TOut>

        where TIn : class
        where TOut : class
{
    public async Task<JobStartedResult> StartJobAsync(JobId jobId, TIn input)
    {
        if (string.IsNullOrWhiteSpace(jobId.Value))
            return new JobStartedResult(false, "Job id is required.", jobId);

        var request = entryFactory.CreateRequest(jobId, input, isStartCommand: true);
        await channel.Writer.WriteAsync(request);

        try
        {
            return await request.Signals.Started.Task.WaitAsync(timeoutOptions.Value.Timeout);
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Job {JobId} did not start within the timeout.", request.Run.JobId.Value);
            return new JobStartedResult(false, "Timeout.", request.Run.JobId);
        }
    }

    public async Task<JobCompletedResult> RunJobAsync(JobId jobId, TIn input)
    {
        if (string.IsNullOrWhiteSpace(jobId.Value))
            return new JobCompletedResult(false, "Job id is required.", jobId);

        var request = entryFactory.CreateRequest(jobId, input, isStartCommand: false);
        await channel.Writer.WriteAsync(request);

        try
        {
            return await request.Signals.Completed.Task.WaitAsync(timeoutOptions.Value.Timeout);
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Job {JobId} did not complete within the timeout.", request.Run.JobId.Value);
            return new JobCompletedResult(false, "Timeout.", request.Run.JobId);
        }
    }

    public Task<JobStoppedResult> StopJobAsync(JobId jobId)
    {
        if (registry.TryGet(jobId, out var entry))
        {
            entry.Signals.Cts.Cancel();
            return Task.FromResult(new JobStoppedResult(true, string.Empty));
        }

        return Task.FromResult(new JobStoppedResult(false, $"Job list does not contain {jobId.Value}"));
    }

    public Task<ActiveJobsQueryResult<TOut>> GetAllJobsAsync()
        => Task.FromResult(stateMapper.Map(registry.GetAll(), registry.Count));

    public Task<ActiveJobsQueryResult<TOut>> GetJobsPageAsync(int skip, int take)
        => Task.FromResult(stateMapper.Map(registry.GetPage(skip, take), registry.Count));

    public Task<ActiveJobsQueryResult<TOut>> GetJobsByIdsAsync(ICollection<JobId> jobIds)
        => Task.FromResult(stateMapper.Map(registry.GetByIds(jobIds), registry.Count));
}
