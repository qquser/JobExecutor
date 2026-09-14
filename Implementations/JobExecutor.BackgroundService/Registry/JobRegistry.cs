using System.Collections.Concurrent;
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Registry;

internal sealed class JobRegistry<TIn, TOut> : IJobRegistry<TIn, TOut>
    where TIn : class
    where TOut : class
{
    private readonly ConcurrentDictionary<JobId, RegisteredJob<TIn, TOut>> _jobs = new();

    public bool Contains(JobId jobId) => _jobs.ContainsKey(jobId);

    public void Add(JobId jobId, RegisteredJob<TIn, TOut> job)
        => _jobs.TryAdd(jobId, job);

    public bool TryGet(JobId jobId, out RegisteredJob<TIn, TOut> job) => _jobs.TryGetValue(jobId, out job!);

    public bool TryRemove(JobId jobId, out RegisteredJob<TIn, TOut> job) => _jobs.TryRemove(jobId, out job!);

    public int Count => _jobs.Count;

    public IReadOnlyCollection<KeyValuePair<JobId, IActiveJob<TIn, TOut>>> GetAll()
        => _jobs.ToArray()
            .Select(kv => new KeyValuePair<JobId, IActiveJob<TIn, TOut>>(kv.Key, kv.Value.Job))
            .ToArray();

    public IReadOnlyCollection<KeyValuePair<JobId, IActiveJob<TIn, TOut>>> GetPage(int skip, int take)
        => _jobs.OrderBy(kv => kv.Key.Value, StringComparer.Ordinal)
            .Skip(skip)
            .Take(take)
            .Select(kv => new KeyValuePair<JobId, IActiveJob<TIn, TOut>>(kv.Key, kv.Value.Job))
            .ToArray();

    public IReadOnlyCollection<KeyValuePair<JobId, IActiveJob<TIn, TOut>>> GetByIds(ICollection<JobId> jobIds)
    {
        var jobs = new List<KeyValuePair<JobId, IActiveJob<TIn, TOut>>>(jobIds.Count);
        foreach (var jobId in jobIds)
        {
            if (_jobs.TryGetValue(jobId, out var job))
                jobs.Add(new(jobId, job.Job));
        }

        return jobs;
    }
}
