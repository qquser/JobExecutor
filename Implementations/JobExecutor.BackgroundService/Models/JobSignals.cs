using JobExecutor.Abstractions.Models;

namespace JobExecutor.BackgroundService.Models;

/// <summary>
/// Coordination primitives for a <see cref="JobRequest{TIn}"/>: the command kind, the per-job
/// cancellation source, and the two signals the caller awaits.
/// </summary>
internal sealed record JobSignals
{
    public required bool IsStartCommand { get; init; }

    public required CancellationTokenSource Cts { get; init; }

    public required TaskCompletionSource<JobStartedResult> Started { get; init; }

    public required TaskCompletionSource<JobCompletedResult> Completed { get; init; }
}
