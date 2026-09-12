using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IJobStateMapper<TIn, TOut>
    where TIn : class
    where TOut : class
{
    RespondWorkersInfo<TOut> Map(IEnumerable<KeyValuePair<string, JobEntry<TIn, TOut>>> subset, int totalCount, long requestId);
}
