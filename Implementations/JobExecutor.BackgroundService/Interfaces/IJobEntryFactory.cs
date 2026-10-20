using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IJobEntryFactory<TIn>
    where TIn : class
{
    StartJobRequest<TIn> CreateStartRequest(JobId jobId, TIn input, CancellationToken token = default);

    RunJobRequest<TIn> CreateRunRequest(JobId jobId, TIn input, CancellationToken token = default);
}
