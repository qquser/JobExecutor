using JobExecutor.Abstractions.Models;

namespace JobExecutor.BackgroundService.Models;

/// <summary>
/// Coordination primitives shared by a pending <see cref="JobRequest{TIn}"/> and the resolved
/// <see cref="JobEntry{TIn,TOut}"/>: the command kind, the creation timestamp, the per-job
/// cancellation source, and the two signals the caller awaits.
/// </summary>
internal sealed record JobSignals
{
    public required bool IsStartCommand { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required CancellationTokenSource Cts { get; init; }

    public required TaskCompletionSource<JobStartedResult> Started { get; init; }

    public required TaskCompletionSource<JobCompletedResult> Completed { get; init; }
}
