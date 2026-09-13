using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IJobRegistry<TIn, TOut>
    where TIn : class
    where TOut : class
{
    bool TryAdd(JobEntry<TIn, TOut> entry);

    bool TryGet(JobId jobId, out JobEntry<TIn, TOut> entry);

    bool TryRemove(JobId jobId, out JobEntry<TIn, TOut> entry);

    int Count { get; }

    IReadOnlyCollection<KeyValuePair<JobId, JobEntry<TIn, TOut>>> GetAll();

    IReadOnlyCollection<KeyValuePair<JobId, JobEntry<TIn, TOut>>> GetPage(int skip, int take);

    IReadOnlyCollection<KeyValuePair<JobId, JobEntry<TIn, TOut>>> GetByIds(ICollection<JobId> jobIds);
}
