namespace JobExecutor.Abstractions.Models.Options;

/// <summary>
/// How long the caller waits for a start/run request before it is considered timed out.
/// Registered as the default via <c>AddBackgroundJobs</c>.
/// </summary>
public sealed record JobTimeoutOptions
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(120);
}
