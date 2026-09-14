using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Registry;

internal sealed class JobEntryFactory<TIn, TOut> : IJobEntryFactory<TIn, TOut>
    where TIn : class
    where TOut : class
{
    public StartJobRequest<TIn> CreateStartRequest(JobId jobId, TIn input)
    {
        return new StartJobRequest<TIn>
        {
            JobId = jobId,
            Input = input,
            Cts = new CancellationTokenSource(),
            Started = new TaskCompletionSource<JobStartedResult>(TaskCreationOptions.RunContinuationsAsynchronously),
        };
    }

    public RunJobRequest<TIn> CreateRunRequest(JobId jobId, TIn input)
    {
        return new RunJobRequest<TIn>
        {
            JobId = jobId,
            Input = input,
            Cts = new CancellationTokenSource(),
            Completed = new TaskCompletionSource<JobCompletedResult>(TaskCreationOptions.RunContinuationsAsynchronously),
        };
    }
}
