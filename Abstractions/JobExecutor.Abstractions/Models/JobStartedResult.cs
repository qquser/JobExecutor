namespace JobExecutor.Abstractions.Models;

public sealed record JobStartedResult(bool Success, string Result, JobId JobId);
