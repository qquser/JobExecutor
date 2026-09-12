namespace JobExecutor.BackgroundService.Models;

/// <summary>
/// Immutable data the <see cref="IJobRunner{TIn,TOut}"/> needs to run a job:
/// the identifier, the input for <see cref="IJob{TIn,TOut}.DoAsync"/>, and the retry policy.
/// </summary>
internal sealed record JobRunModel<TIn>
    where TIn : class
{
    public required string JobId { get; init; }
    public required TIn Input { get; init; }
    public required int MaxNrOfRetries { get; init; }
    public required TimeSpan Backoff { get; init; }
}
