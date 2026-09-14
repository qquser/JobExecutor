using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Queries;
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

    ActiveJobsQueryResult<TOut> GetAll();

    ActiveJobsQueryResult<TOut> GetPage(int skip, int take);

    ActiveJobsQueryResult<TOut> GetByIds(ICollection<JobId> jobIds);
}
