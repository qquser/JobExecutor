using System.Threading.Channels;
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService;

internal sealed class BackgroundJobContext<TIn, TOut> : IJobContext<TIn, TOut>
    where TIn : class
    where TOut : class
{
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(120);
    private readonly Channel<JobEntry<TIn, TOut>> _channel;
    private readonly IJobEntryFactory<TIn, TOut> _entryFactory;
    private readonly IJobRegistry<TIn, TOut> _registry;
    private readonly IJobStateMapper<TIn, TOut> _stateMapper;

    public BackgroundJobContext(
        Channel<JobEntry<TIn, TOut>> channel,
        IJobEntryFactory<TIn, TOut> entryFactory,
        IJobRegistry<TIn, TOut> registry,
        IJobStateMapper<TIn, TOut> stateMapper)
    {
        _channel = channel;
        _entryFactory = entryFactory;
        _registry = registry;
        _stateMapper = stateMapper;
    }

    public async Task<JobCreatedCommandResult> CreateJobAsync(string jobId, TIn input,
        int? maxNrOfRetries = null, TimeSpan? minBackoff = null, TimeSpan? maxBackoff = null, TimeSpan? timeout = null)
    {
        var entry = _entryFactory.Create(jobId, input, maxNrOfRetries, minBackoff, isCreateCommand: true);
        await _channel.Writer.WriteAsync(entry);

        try
        {
            return await entry.Created.Task.WaitAsync(timeout ?? _defaultTimeout);
        }
        catch (TimeoutException)
        {
            return new JobCreatedCommandResult(false, "Timeout.", entry.Run.JobId);
        }
    }

    public async Task<JobDoneCommandResult> DoJobAsync(string jobId, TIn input,
        int? maxNrOfRetries = null, TimeSpan? minBackoff = null, TimeSpan? maxBackoff = null, TimeSpan? timeout = null)
    {
        var entry = _entryFactory.Create(jobId, input, maxNrOfRetries, minBackoff, isCreateCommand: false);
        await _channel.Writer.WriteAsync(entry);

        try
        {
            return await entry.Done.Task.WaitAsync(timeout ?? _defaultTimeout);
        }
        catch (TimeoutException)
        {
            return new JobDoneCommandResult(false, "Timeout.", entry.Run.JobId);
        }
    }

    public Task<StopJobCommandResult> StopJobAsync(string jobId, TimeSpan? timeout = null)
    {
        if (_registry.TryGet(jobId, out var entry))
        {
            entry.Cts.Cancel();
            return Task.FromResult(new StopJobCommandResult(true, string.Empty));
        }

        return Task.FromResult(new StopJobCommandResult(false, $"Job list does not contain {jobId}"));
    }

    public Task<RespondWorkersInfo<TOut>> GetAllJobsAsync(TimeSpan? timeout = null, long requestId = 0)
        => Task.FromResult(_stateMapper.Map(_registry.GetAll(), _registry.Count, requestId));

    public Task<RespondWorkersInfo<TOut>> GetJobsPaginateAsync(int skip, int take, TimeSpan? timeout = null, long requestId = 0)
        => Task.FromResult(_stateMapper.Map(_registry.GetPage(skip, take), _registry.Count, requestId));

    public Task<RespondWorkersInfo<TOut>> GetJobsByIdsAsync(ICollection<string> jobIds, TimeSpan? timeout = null, long requestId = 0)
        => Task.FromResult(_stateMapper.Map(_registry.GetByIds(jobIds), _registry.Count, requestId));
}
