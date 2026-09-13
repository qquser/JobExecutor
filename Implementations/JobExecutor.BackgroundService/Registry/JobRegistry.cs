using System.Collections.Concurrent;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Registry;

internal sealed class JobRegistry<TIn, TOut> : IJobRegistry<TIn, TOut>
    where TIn : class
    where TOut : class
{
    private readonly ConcurrentDictionary<JobId, JobEntry<TIn, TOut>> _entries = new();

    public bool TryAdd(JobEntry<TIn, TOut> entry) => _entries.TryAdd(entry.Run.JobId, entry);

    public bool TryGet(JobId jobId, out JobEntry<TIn, TOut> entry) => _entries.TryGetValue(jobId, out entry!);

    public bool TryRemove(JobId jobId, out JobEntry<TIn, TOut> entry) => _entries.TryRemove(jobId, out entry!);

    public int Count => _entries.Count;

    public IReadOnlyCollection<KeyValuePair<JobId, JobEntry<TIn, TOut>>> GetAll() => _entries.ToArray();

    public IReadOnlyCollection<KeyValuePair<JobId, JobEntry<TIn, TOut>>> GetPage(int skip, int take)
        => _entries.OrderBy(kv => kv.Value.Signals.CreatedAt)
            .ThenBy(kv => kv.Key.Value, StringComparer.Ordinal)
            .Skip(skip)
            .Take(take)
            .ToArray();

    public IReadOnlyCollection<KeyValuePair<JobId, JobEntry<TIn, TOut>>> GetByIds(ICollection<JobId> jobIds)
    {
        var entries = new List<KeyValuePair<JobId, JobEntry<TIn, TOut>>>(jobIds.Count);
        foreach (var jobId in jobIds)
        {
            if (_entries.TryGetValue(jobId, out var entry))
                entries.Add(new(jobId, entry));
        }

        return entries;
    }
}
