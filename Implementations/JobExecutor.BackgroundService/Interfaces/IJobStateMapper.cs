using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Queries;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IJobStateMapper<TIn, TOut>
    where TIn : class
    where TOut : class
{
    ActiveJobsQueryResult<TOut> Map(IEnumerable<KeyValuePair<JobId, JobEntry<TIn, TOut>>> subset, int totalCount);
}
