using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IJobEntryFactory<TIn, TOut>
    where TIn : class
    where TOut : class
{
    JobRequest<TIn> CreateRequest(JobId jobId, TIn input, bool isStartCommand);

    JobEntry<TIn, TOut> CreateEntry(JobRequest<TIn> request, IActiveJob<TIn, TOut> job);
}
