using JobExecutor.Abstractions.Models;

namespace JobExecutor.BackgroundService.Models;

/// <summary>
/// A run command that has been submitted but not yet processed by the run engine.
/// The caller awaits <see cref="Completed"/> to receive the final result.
/// </summary>
internal sealed record RunJobRequest<TIn>
    where TIn : class
{
    public required JobId JobId { get; init; }

    public required TIn Input { get; init; }

    public required CancellationTokenSource Cts { get; init; }

    public required TaskCompletionSource<JobCompletedResult> Completed { get; init; }
}
