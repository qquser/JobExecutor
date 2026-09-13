namespace JobExecutor.Abstractions.Models.Options;

/// <summary>
/// Retry policy for a background job. Used as the default when registered via
/// <c>AddBackgroundJobs</c>, and as a per-call override passed to
/// <see cref="Interfaces.IJobManager{TIn,TOut}.StartJobAsync"/> / <c>RunJobAsync</c>.
/// </summary>
public sealed record JobRetryOptions
{
    /// <summary>
    /// The first attempt plus this many retries, so IJob.DoAsync is invoked at most
    /// MaxNrOfRetries + 1 times when exceptions occur.
    /// </summary>
    public int MaxNrOfRetries { get; init; } = 5;

    /// <summary>
    /// The delay before the next retry after an error.
    /// </summary>
    public TimeSpan MinBackoff { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Reserved for future jitter between <see cref="MinBackoff"/> and this value. Not applied yet.
    /// </summary>
    public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromSeconds(3);
}
