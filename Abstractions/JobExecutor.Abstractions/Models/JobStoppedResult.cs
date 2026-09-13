namespace JobExecutor.Abstractions.Models;

public sealed record JobStoppedResult(bool Success, string Result);
