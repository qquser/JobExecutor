using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Options;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IJobEntryFactory<TIn, TOut>
    where TIn : class
    where TOut : class
{
    JobRequest<TIn> CreateRequest(string jobId, TIn input, JobRetryOptions? retry, bool isStartCommand);

    JobEntry<TIn, TOut> CreateEntry(JobRequest<TIn> request, IJob<TIn, TOut> job);
}
