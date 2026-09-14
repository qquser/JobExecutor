using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Queries;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Registry;

internal sealed class JobStateMapper<TIn, TOut> : IJobStateMapper<TIn, TOut>
    where TIn : class
    where TOut : class
{
    public ActiveJobsQueryResult<TOut> Map(IEnumerable<KeyValuePair<JobId, JobEntry<TIn, TOut>>> subset,
        int totalCount)
    {
        var data = subset.ToDictionary(
            kv => kv.Key,
            kv => new ActiveJobStateInfo<TOut>
            {
                Success = true,
                ErrorMessage = string.Empty,
                Result = kv.Value.Job.GetCurrentState(),
            });

        return new ActiveJobsQueryResult<TOut>
        {
            Jobs = data,
            TotalCount = totalCount,
        };
    }
}
