namespace JobExecutor.Api.Contracts;

/// <summary>
/// A request with the identifiers of the jobs to query.
/// </summary>
public sealed record JobIdsRequest
{
    /// <summary>
    /// The identifiers of the jobs to query.
    /// </summary>
    public IReadOnlyList<string> JobIds { get; init; } = Array.Empty<string>();
}
