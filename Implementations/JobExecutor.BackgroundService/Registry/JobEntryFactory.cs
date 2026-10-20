using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Registry;

internal sealed class JobEntryFactory<TIn> : IJobEntryFactory<TIn>
    where TIn : class
{
    public StartJobRequest<TIn> CreateStartRequest(JobId jobId, TIn input, CancellationToken callerToken = default)
    {
        return new StartJobRequest<TIn>
        {
            JobId = jobId,
            Input = input,
            StopSource = new CancellationTokenSource(),
            CallerToken = callerToken,
            Started = new TaskCompletionSource<JobStartedResult>(TaskCreationOptions.RunContinuationsAsynchronously),
        };
    }

    public RunJobRequest<TIn> CreateRunRequest(JobId jobId, TIn input, CancellationToken callerToken = default)
    {
        return new RunJobRequest<TIn>
        {
            JobId = jobId,
            Input = input,
            StopSource = new CancellationTokenSource(),
            CallerToken = callerToken,
            Started = new TaskCompletionSource<JobStartedResult>(TaskCreationOptions.RunContinuationsAsynchronously),
            Completed = new TaskCompletionSource<JobCompletedResult>(TaskCreationOptions.RunContinuationsAsynchronously),
        };
    }
}
