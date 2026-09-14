using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IJobRegistry<TIn, TOut>
    where TIn : class
    where TOut : class
{
    bool Contains(JobId jobId);

    void Add(JobId jobId, RegisteredJob<TIn, TOut> job);

    bool TryGet(JobId jobId, out RegisteredJob<TIn, TOut> job);

    bool TryRemove(JobId jobId, out RegisteredJob<TIn, TOut> job);

    int Count { get; }

    IReadOnlyCollection<KeyValuePair<JobId, IActiveJob<TIn, TOut>>> GetAll();

    IReadOnlyCollection<KeyValuePair<JobId, IActiveJob<TIn, TOut>>> GetPage(int skip, int take);

    IReadOnlyCollection<KeyValuePair<JobId, IActiveJob<TIn, TOut>>> GetByIds(ICollection<JobId> jobIds);
}
