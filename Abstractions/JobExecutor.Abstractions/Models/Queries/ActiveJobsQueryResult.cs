using JobExecutor.Abstractions.Models;

namespace JobExecutor.Abstractions.Models.Queries;

public sealed record ActiveJobsQueryResult<TOut>
    where TOut : class
{
    public required IReadOnlyDictionary<JobId, ActiveJobStateInfo<TOut>> Jobs { get; init; }

    public required int TotalCount { get; init; }
}
