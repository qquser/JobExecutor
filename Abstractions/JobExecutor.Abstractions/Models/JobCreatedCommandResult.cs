namespace JobExecutor.Abstractions.Models;

public sealed record JobCreatedCommandResult(bool Success, string Result, string JobId);
