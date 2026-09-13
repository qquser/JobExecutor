using JobExecutor.Abstractions.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IJobEntryFactory<TIn, TOut>
    where TIn : class
    where TOut : class
{
    JobRequest<TIn> CreateRequest(string jobId, TIn input, bool isStartCommand);

    JobEntry<TIn, TOut> CreateEntry(JobRequest<TIn> request, IJob<TIn, TOut> job);
}
