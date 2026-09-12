using JobExecutor.Abstractions.Models;

namespace JobExecutor.BackgroundService.Models;

/// <summary>
/// Coordination primitives shared by a pending <see cref="JobRequest{TIn}"/> and the resolved
/// <see cref="JobEntry{TIn,TOut}"/>: the command kind, the creation timestamp, the per-job
/// cancellation source, and the two completion signals the caller awaits.
/// </summary>
internal sealed record JobSignals
{
    public required bool IsCreateCommand { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required CancellationTokenSource Cts { get; init; }

    public required TaskCompletionSource<JobCreatedCommandResult> Created { get; init; }

    public required TaskCompletionSource<JobDoneCommandResult> Done { get; init; }
}
