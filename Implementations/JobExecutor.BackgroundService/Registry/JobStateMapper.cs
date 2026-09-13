using JobExecutor.Abstractions.Models.Queries;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Registry;

internal sealed class JobStateMapper<TIn, TOut> : IJobStateMapper<TIn, TOut>
    where TIn : class
    where TOut : class
{
    public JobsQueryResult<TOut> Map(IEnumerable<KeyValuePair<string, JobEntry<TIn, TOut>>> subset,
        int totalCount)
    {
        var data = subset.ToDictionary(
            kv => kv.Key,
            kv => new JobStateInfo<TOut>
            {
                Success = true,
                ErrorMessage = string.Empty,
                Result = kv.Value.Job.GetCurrentState(),
            });

        return new JobsQueryResult<TOut>
        {
            Jobs = data,
            TotalCount = totalCount,
        };
    }
}
