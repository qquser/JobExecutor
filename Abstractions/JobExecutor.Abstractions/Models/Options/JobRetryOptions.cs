namespace JobExecutor.Abstractions.Models.Options;

/// <summary>
/// Retry policy for a background job, configured once at registration via <c>AddBackgroundJobs</c>.
/// </summary>
public sealed record JobRetryOptions
{
    /// <summary>
    /// The first attempt plus this many retries, so IActiveJob.DoAsync is invoked at most
    /// MaxNrOfRetries + 1 times when exceptions occur.
    /// </summary>
    public int MaxNrOfRetries { get; set; } = 5;

    /// <summary>
    /// The delay before the next retry after an error.
    /// </summary>
    public TimeSpan MinBackoff { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Reserved for future jitter between <see cref="MinBackoff"/> and this value. Not applied yet.
    /// </summary>
    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromSeconds(3);
}
