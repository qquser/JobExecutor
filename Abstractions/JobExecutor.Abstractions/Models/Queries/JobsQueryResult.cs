namespace JobExecutor.Abstractions.Models.Queries;

public sealed record JobsQueryResult<TOut>
    where TOut : class
{
    public required Dictionary<string, JobStateInfo<TOut>> Jobs { get; init; }

    public required int TotalCount { get; init; }
}
