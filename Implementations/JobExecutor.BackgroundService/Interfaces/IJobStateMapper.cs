using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Queries;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IJobStateMapper<TIn, TOut>
    where TIn : class
    where TOut : class
{
    JobsQueryResult<TOut> Map(IEnumerable<KeyValuePair<string, JobEntry<TIn, TOut>>> subset, int totalCount, long requestId);
}
