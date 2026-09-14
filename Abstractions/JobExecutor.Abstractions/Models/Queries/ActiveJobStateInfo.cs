namespace JobExecutor.Abstractions.Models.Queries;

public sealed record ActiveJobStateInfo<TOut>
    where TOut : class
{
    public required TOut Result { get; init; }
}
