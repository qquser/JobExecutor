using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Options;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using Microsoft.Extensions.Options;

namespace JobExecutor.BackgroundService.Registry;

internal sealed class JobEntryFactory<TIn, TOut>(IOptions<JobRetryOptions> retryOptions)
    : IJobEntryFactory<TIn, TOut>
    where TIn : class
    where TOut : class
{
    public JobRequest<TIn> CreateRequest(string jobId, TIn input, JobRetryOptions? retry, bool isStartCommand)
    {
        var id = string.IsNullOrWhiteSpace(jobId) ? Guid.NewGuid().ToString() : jobId;
        var effective = retry ?? retryOptions.Value;

        return new JobRequest<TIn>
        {
            Run = new JobRunModel<TIn>
            {
                JobId = id,
                Input = input,
                MaxNrOfRetries = effective.MaxNrOfRetries,
                Backoff = effective.MinBackoff,
            },
            Signals = new JobSignals
            {
                IsStartCommand = isStartCommand,
                CreatedAt = DateTimeOffset.UtcNow,
                Cts = new CancellationTokenSource(),
                Started = new TaskCompletionSource<JobStartedResult>(TaskCreationOptions.RunContinuationsAsynchronously),
                Completed = new TaskCompletionSource<JobCompletedResult>(TaskCreationOptions.RunContinuationsAsynchronously),
            },
        };
    }

    public JobEntry<TIn, TOut> CreateEntry(JobRequest<TIn> request, IJob<TIn, TOut> job)
        => new JobEntry<TIn, TOut>
        {
            Run = request.Run,
            Signals = request.Signals,
            Job = job,
        };
}
