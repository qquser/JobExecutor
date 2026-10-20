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

    /// <summary>
    /// The source that stops the job from the outside: <c>StopJobAsync</c> and the timeout of the
    /// manager cancel it. The job is linked to it together with <see cref="CallerToken"/>.
    /// </summary>
    public required CancellationTokenSource StopSource { get; init; }

    /// <summary>
    /// The token of the caller, the one that was passed to <c>StartJobAsync</c>. The job is cancelled
    /// when it is cancelled.
    /// </summary>
    public CancellationToken CallerToken { get; init; }

    public required TaskCompletionSource<JobStartedResult> Started { get; init; }
}
