using JobExecutor.Abstractions.Models;

namespace JobExecutor.BackgroundService.Models;

/// <summary>
/// A start command that has been submitted but not yet processed by the start engine.
/// The caller awaits <see cref="Started"/> to learn that the job has been registered and started.
/// </summary>
internal sealed record StartJobRequest<TIn>
    where TIn : class
{
    public required JobId JobId { get; init; }

    public required TIn Input { get; init; }

    public required CancellationTokenSource Cts { get; init; }

    public required TaskCompletionSource<JobStartedResult> Started { get; init; }
}
