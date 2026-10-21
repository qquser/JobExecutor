using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Queries;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Registry;

[SuppressMessage(
    "ReSharper",
    "ConditionIsAlwaysTrueOrFalseAccordingToNullableApiContract",
    Justification = "The identifiers come from the caller and can be missing.")]
internal sealed class JobRegistry<TIn, TOut>(IJobStateMapper<TIn, TOut> stateMapper) : IJobRegistry<TIn, TOut>
    where TIn : class
    where TOut : class
{
    private readonly ConcurrentDictionary<JobId, RegisteredJob<TIn, TOut>> _jobs = new();

    public bool Contains(JobId jobId) => _jobs.ContainsKey(jobId);

    public bool TryAdd(JobId jobId, RegisteredJob<TIn, TOut> job)
        => _jobs.TryAdd(jobId, job);

    public bool TryGet(JobId jobId, out RegisteredJob<TIn, TOut> job) => _jobs.TryGetValue(jobId, out job!);

    public bool TryRemove(JobId jobId, out RegisteredJob<TIn, TOut> job) => _jobs.TryRemove(jobId, out job!);

    public ActiveJobsQueryResult<TOut> GetAll()
    {
        var snapshot = _jobs.ToArray();

        return stateMapper.Map(
            snapshot.Select(kv => new KeyValuePair<JobId, IActiveJob<TIn, TOut>>(kv.Key, kv.Value.Job)),
            snapshot.Length);
    }

    public ActiveJobsQueryResult<TOut> GetPage(int skip, int take)
    {
        if (skip < 0 || take < 0)
        {
            var empty = _jobs.ToArray();

            return stateMapper.Map(
                Array.Empty<KeyValuePair<JobId, IActiveJob<TIn, TOut>>>(),
                empty.Length);
        }

        var snapshot = _jobs.ToArray();

        return stateMapper.Map(
            snapshot.OrderBy(kv => kv.Key.Value, StringComparer.Ordinal)
                .Skip(skip)
                .Take(take)
                .Select(kv => new KeyValuePair<JobId, IActiveJob<TIn, TOut>>(kv.Key, kv.Value.Job)),
            snapshot.Length);
    }

    public ActiveJobsQueryResult<TOut> GetByIds(ICollection<JobId> jobIds)
    {
        var snapshot = _jobs.ToArray();

        if (jobIds is null || jobIds.Count == 0)
            return stateMapper.Map(Array.Empty<KeyValuePair<JobId, IActiveJob<TIn, TOut>>>(), snapshot.Length);

        var jobs = new List<KeyValuePair<JobId, IActiveJob<TIn, TOut>>>(jobIds.Count);
        var seen = new HashSet<JobId>();

        foreach (var jobId in jobIds)
        {
            if (jobId is null || !seen.Add(jobId))
                continue;

            if (_jobs.TryGetValue(jobId, out var job))
                jobs.Add(new(jobId, job.Job));
        }

        return stateMapper.Map(jobs, snapshot.Length);
    }
}
