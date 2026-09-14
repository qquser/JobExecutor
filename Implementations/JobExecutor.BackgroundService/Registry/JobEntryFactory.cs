using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Registry;

internal sealed class JobEntryFactory<TIn, TOut> : IJobEntryFactory<TIn, TOut>
    where TIn : class
    where TOut : class
{
    public JobRequest<TIn> CreateRequest(JobId jobId, TIn input, bool isStartCommand)
    {
        return new JobRequest<TIn>
        {
            JobId = jobId,
            Input = input,
            Signals = new JobSignals
            {
                IsStartCommand = isStartCommand,
                Cts = new CancellationTokenSource(),
                Started = new TaskCompletionSource<JobStartedResult>(TaskCreationOptions.RunContinuationsAsynchronously),
                Completed = new TaskCompletionSource<JobCompletedResult>(TaskCreationOptions.RunContinuationsAsynchronously),
            },
        };
    }
}
