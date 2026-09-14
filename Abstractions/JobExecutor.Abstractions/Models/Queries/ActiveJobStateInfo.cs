namespace JobExecutor.Abstractions.Models.Queries;

public sealed record ActiveJobStateInfo<TOut>
    where TOut : class
{
    public required bool Success { get; init; }

    public required string ErrorMessage { get; init; }

    public required TOut? Result { get; init; }
}
