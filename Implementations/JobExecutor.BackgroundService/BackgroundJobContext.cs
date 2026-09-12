using System.Threading.Channels;
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using Microsoft.Extensions.Logging;

namespace JobExecutor.BackgroundService;

internal sealed class BackgroundJobContext<TIn, TOut>(
    Channel<JobRequest<TIn>> channel,
    IJobEntryFactory<TIn, TOut> entryFactory,
    IJobRegistry<TIn, TOut> registry,
    IJobStateMapper<TIn, TOut> stateMapper,
    ILogger<BackgroundJobContext<TIn, TOut>> logger)
    : IJobContext<TIn, TOut>
    where TIn : class
    where TOut : class
{
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(120);

    public async Task<JobCreatedCommandResult> CreateJobAsync(string jobId, TIn input,
        int? maxNrOfRetries = null, TimeSpan? minBackoff = null, TimeSpan? maxBackoff = null, TimeSpan? timeout = null)
    {
        var request = entryFactory.CreateRequest(jobId, input, maxNrOfRetries, minBackoff, isCreateCommand: true);
        await channel.Writer.WriteAsync(request);

        try
        {
            return await request.Signals.Created.Task.WaitAsync(timeout ?? _defaultTimeout);
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Job {JobId} did not complete within the timeout.", request.Run.JobId);
            return new JobCreatedCommandResult(false, "Timeout.", request.Run.JobId);
        }
    }

    public async Task<JobDoneCommandResult> DoJobAsync(string jobId, TIn input,
        int? maxNrOfRetries = null, TimeSpan? minBackoff = null, TimeSpan? maxBackoff = null, TimeSpan? timeout = null)
    {
        var request = entryFactory.CreateRequest(jobId, input, maxNrOfRetries, minBackoff, isCreateCommand: false);
        await channel.Writer.WriteAsync(request);

        try
        {
            return await request.Signals.Done.Task.WaitAsync(timeout ?? _defaultTimeout);
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Job {JobId} did not complete within the timeout.", request.Run.JobId);
            return new JobDoneCommandResult(false, "Timeout.", request.Run.JobId);
        }
    }

    public Task<StopJobCommandResult> StopJobAsync(string jobId, TimeSpan? timeout = null)
    {
        if (registry.TryGet(jobId, out var entry))
        {
            entry.Signals.Cts.Cancel();
            return Task.FromResult(new StopJobCommandResult(true, string.Empty));
        }

        return Task.FromResult(new StopJobCommandResult(false, $"Job list does not contain {jobId}"));
    }

    public Task<RespondWorkersInfo<TOut>> GetAllJobsAsync(TimeSpan? timeout = null, long requestId = 0)
        => Task.FromResult(stateMapper.Map(registry.GetAll(), registry.Count, requestId));

    public Task<RespondWorkersInfo<TOut>> GetJobsPaginateAsync(int skip, int take, TimeSpan? timeout = null, long requestId = 0)
        => Task.FromResult(stateMapper.Map(registry.GetPage(skip, take), registry.Count, requestId));

    public Task<RespondWorkersInfo<TOut>> GetJobsByIdsAsync(ICollection<string> jobIds, TimeSpan? timeout = null, long requestId = 0)
        => Task.FromResult(stateMapper.Map(registry.GetByIds(jobIds), registry.Count, requestId));
}
