using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Queries;
using JobExecutor.BackgroundService.Interfaces;

namespace JobExecutor.BackgroundService.Registry.Mapper;

internal sealed class JobStateMapper<TIn, TOut> : IJobStateMapper<TIn, TOut>
    where TIn : class
    where TOut : class
{
    public ActiveJobsQueryResult<TOut> Map(IEnumerable<KeyValuePair<JobId, IActiveJob<TIn, TOut>>> subset,
        int totalCount)
    {
        var data = subset.ToDictionary(
            kv => kv.Key,
            kv => new ActiveJobStateInfo<TOut>
            {
                Result = kv.Value.GetCurrentState(),
            });

        return new ActiveJobsQueryResult<TOut>
        {
            Jobs = data,
            TotalCount = totalCount,
        };
    }
}
