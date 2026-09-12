using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService;

internal sealed class JobEntryFactory<TIn, TOut> : IJobEntryFactory<TIn, TOut>
    where TIn : class
    where TOut : class
{
    private readonly int _defaultMaxNrOfRetries = 5;
    private readonly TimeSpan _defaultBackoff = TimeSpan.FromSeconds(1);

    public JobEntry<TIn, TOut> Create(string jobId, 
        TIn input, int? maxNrOfRetries, 
        TimeSpan? minBackoff, bool isCreateCommand)
    {
        var id = string.IsNullOrWhiteSpace(jobId) ? Guid.NewGuid().ToString() : jobId;

        return new JobEntry<TIn, TOut>
        {
            Run = new JobRunModel<TIn>
            {
                JobId = id,
                Input = input,
                MaxNrOfRetries = maxNrOfRetries ?? _defaultMaxNrOfRetries,
                Backoff = minBackoff ?? _defaultBackoff,
            },
            IsCreateCommand = isCreateCommand,
            CreatedAt = DateTimeOffset.UtcNow,
            Cts = new CancellationTokenSource(),
            Created = new TaskCompletionSource<JobCreatedCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously),
            Done = new TaskCompletionSource<JobDoneCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously),
        };
    }
}
