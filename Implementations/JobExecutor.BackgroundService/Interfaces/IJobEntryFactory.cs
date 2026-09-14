using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IJobEntryFactory<TIn, TOut>
    where TIn : class
    where TOut : class
{
    StartJobRequest<TIn> CreateStartRequest(JobId jobId, TIn input);

    RunJobRequest<TIn> CreateRunRequest(JobId jobId, TIn input);
}
