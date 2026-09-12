using System.Collections.Concurrent;
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService;

internal sealed class JobRegistry<TIn, TOut> : IJobRegistry<TIn, TOut>
    where TIn : class
    where TOut : class
{
    private readonly ConcurrentDictionary<string, JobEntry<TIn, TOut>> _entries = new();

    public JobRegistry()
    {
    }

    public bool TryAdd(JobEntry<TIn, TOut> entry) => _entries.TryAdd(entry.Run.JobId, entry);

    public bool TryGet(string jobId, out JobEntry<TIn, TOut> entry) => _entries.TryGetValue(jobId, out entry!);

    public bool TryRemove(string jobId, out JobEntry<TIn, TOut> entry) => _entries.TryRemove(jobId, out entry!);

    public void AttachJob(string jobId, IJob<TIn, TOut> job) => _entries[jobId].Job = job;

    public int Count => _entries.Count;

    public IReadOnlyCollection<KeyValuePair<string, JobEntry<TIn, TOut>>> GetAll() => _entries.ToArray();

    public IReadOnlyCollection<KeyValuePair<string, JobEntry<TIn, TOut>>> GetPage(int skip, int take)
        => _entries.Skip(skip).Take(take).ToArray();

    public IReadOnlyCollection<KeyValuePair<string, JobEntry<TIn, TOut>>> GetByIds(ICollection<string> jobIds)
    {
        var entries = new List<KeyValuePair<string, JobEntry<TIn, TOut>>>(jobIds.Count);
        foreach (var jobId in jobIds)
        {
            if (_entries.TryGetValue(jobId, out var entry))
                entries.Add(new(jobId, entry));
        }

        return entries;
    }
}
