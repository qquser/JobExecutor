namespace JobExecutor.Abstractions.Models;

/// <summary>
/// Strongly-typed identifier of a background job. Identifies a job uniquely among running jobs.
/// </summary>
public sealed record JobId(string Value);
