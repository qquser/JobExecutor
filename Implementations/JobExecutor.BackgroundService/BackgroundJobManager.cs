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

    : IJobManager<TIn, TOut>

        where TIn : class
        where TOut : class
{
    public async Task<JobStartedResult> StartJobAsync(string jobId, TIn input,
        JobRetryOptions? retry = null, TimeSpan? timeout = null)
    {
        var request = entryFactory.CreateRequest(jobId, input, retry, isStartCommand: true);
        await channel.Writer.WriteAsync(request);

        try
        {
            return await request.Signals.Started.Task.WaitAsync(timeout ?? timeoutOptions.Value.Timeout);
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Job {JobId} did not start within the timeout.", request.Run.JobId);
            return new JobStartedResult(false, "Timeout.", request.Run.JobId);
        }
    }

    public async Task<JobCompletedResult> RunJobAsync(string jobId, TIn input,
        JobRetryOptions? retry = null, TimeSpan? timeout = null)
    {
        var request = entryFactory.CreateRequest(jobId, input, retry, isStartCommand: false);
        await channel.Writer.WriteAsync(request);

        try
        {
            return await request.Signals.Completed.Task.WaitAsync(timeout ?? timeoutOptions.Value.Timeout);
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Job {JobId} did not complete within the timeout.", request.Run.JobId);
            return new JobCompletedResult(false, "Timeout.", request.Run.JobId);
        }
    }

    public Task<JobStoppedResult> StopJobAsync(string jobId)
    {
        if (registry.TryGet(jobId, out var entry))
        {
            entry.Signals.Cts.Cancel();
            return Task.FromResult(new JobStoppedResult(true, string.Empty));
        }

        return Task.FromResult(new JobStoppedResult(false, $"Job list does not contain {jobId}"));
    }

    public Task<JobsQueryResult<TOut>> GetAllJobsAsync()
        => Task.FromResult(stateMapper.Map(registry.GetAll(), registry.Count));

    public Task<JobsQueryResult<TOut>> GetJobsPageAsync(int skip, int take)
        => Task.FromResult(stateMapper.Map(registry.GetPage(skip, take), registry.Count));

    public Task<JobsQueryResult<TOut>> GetJobsByIdsAsync(ICollection<string> jobIds)
        => Task.FromResult(stateMapper.Map(registry.GetByIds(jobIds), registry.Count));
}
