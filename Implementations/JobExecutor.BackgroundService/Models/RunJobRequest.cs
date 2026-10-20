using JobExecutor.Abstractions.Models;

namespace JobExecutor.BackgroundService.Models;

/// <summary>
/// A run command that has been submitted but not yet processed by the run engine.
/// The caller awaits <see cref="Started"/> to learn that the job has begun to execute and
/// <see cref="Completed"/> to receive the final result.
/// </summary>
internal sealed record RunJobRequest<TIn>
    where TIn : class
{
    public required JobId JobId { get; init; }

    public required TIn Input { get; init; }

    /// <summary>
    /// The source that stops the job from the outside: <c>StopJobAsync</c> and the timeouts of the
    /// manager cancel it. The job is linked to it together with <see cref="CallerToken"/>.
    /// </summary>
    public required CancellationTokenSource StopSource { get; init; }

    /// <summary>
    /// The token of the caller, the one that was passed to <c>RunJobAsync</c>. The job is cancelled
    /// when it is cancelled.
    /// </summary>
    public CancellationToken CallerToken { get; init; }

    /// <summary>
    /// The result of the start of the job. It is set when the job begins to execute, or when the
    /// request cannot be executed at all.
    /// </summary>
    public required TaskCompletionSource<JobStartedResult> Started { get; init; }

    public required TaskCompletionSource<JobCompletedResult> Completed { get; init; }
}
