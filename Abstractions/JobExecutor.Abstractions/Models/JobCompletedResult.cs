namespace JobExecutor.Abstractions.Models;

public sealed record JobCompletedResult(bool Success, string Result, string JobId);
