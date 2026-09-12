using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Registry;

internal sealed class JobStateMapper<TIn, TOut> : IJobStateMapper<TIn, TOut>
    where TIn : class
    where TOut : class
{
    public RespondWorkersInfo<TOut> Map(IEnumerable<KeyValuePair<string, JobEntry<TIn, TOut>>> subset,
        int totalCount, long requestId)
    {
        var data = subset.ToDictionary(
            kv => kv.Key,
            kv => new ReplyWorkerInfo<TOut>(kv.Value.Job.GetCurrentState(kv.Key)));

        return new RespondWorkersInfo<TOut>(requestId, data, totalCount);
    }
}
