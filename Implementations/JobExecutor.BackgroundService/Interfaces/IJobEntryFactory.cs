using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IJobEntryFactory<TIn, TOut>
    where TIn : class
    where TOut : class
{
    JobEntry<TIn, TOut> Create(string jobId, TIn input, int? maxNrOfRetries, TimeSpan? minBackoff, bool isCreateCommand);
}
