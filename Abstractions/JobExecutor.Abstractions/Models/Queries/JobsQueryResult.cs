namespace JobExecutor.Abstractions.Models.Queries;

public sealed record JobsQueryResult<TOut>
    where TOut : class
{
    public required long RequestId { get; init; }

    public required Dictionary<string, JobStateInfo<TOut>> Jobs { get; init; }

    public required int TotalCount { get; init; }
}
