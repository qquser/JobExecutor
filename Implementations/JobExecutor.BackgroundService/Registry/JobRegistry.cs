using System.Collections.Concurrent;
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Queries;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace JobExecutor.BackgroundService.Registry;

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
        => stateMapper.Map(
            _jobs.ToArray().Select(kv => new KeyValuePair<JobId, IActiveJob<TIn, TOut>>(kv.Key, kv.Value.Job)),
            _jobs.Count);

    public ActiveJobsQueryResult<TOut> GetPage(int skip, int take)
        => stateMapper.Map(
            _jobs.OrderBy(kv => kv.Key.Value, StringComparer.Ordinal)
                .Skip(skip)
                .Take(take)
                .Select(kv => new KeyValuePair<JobId, IActiveJob<TIn, TOut>>(kv.Key, kv.Value.Job)),
            _jobs.Count);

    public ActiveJobsQueryResult<TOut> GetByIds(ICollection<JobId> jobIds)
    {
        if (jobIds is null || jobIds.Count == 0)
            return stateMapper.Map(Array.Empty<KeyValuePair<JobId, IActiveJob<TIn, TOut>>>(), _jobs.Count);

        var jobs = new List<KeyValuePair<JobId, IActiveJob<TIn, TOut>>>(jobIds.Count);
        foreach (var jobId in jobIds)
        {
            if (_jobs.TryGetValue(jobId, out var job))
                jobs.Add(new(jobId, job.Job));
        }

        return stateMapper.Map(jobs, _jobs.Count);
    }
}
