using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IJobRegistry<TIn, TOut>
    where TIn : class
    where TOut : class
{
    bool TryAdd(JobEntry<TIn, TOut> entry);

    bool TryGet(string jobId, out JobEntry<TIn, TOut> entry);

    bool TryRemove(string jobId, out JobEntry<TIn, TOut> entry);

    int Count { get; }

    IReadOnlyCollection<KeyValuePair<string, JobEntry<TIn, TOut>>> GetAll();

    IReadOnlyCollection<KeyValuePair<string, JobEntry<TIn, TOut>>> GetPage(int skip, int take);

    IReadOnlyCollection<KeyValuePair<string, JobEntry<TIn, TOut>>> GetByIds(ICollection<string> jobIds);
}
