namespace JobExecutor.Abstractions.Models;

public sealed record StopJobCommandResult(bool Success, string Result);
