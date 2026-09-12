namespace JobExecutor.Abstractions.Models;

public sealed record JobDoneCommandResult(bool Success, string Result, string JobId);
