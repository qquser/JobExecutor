namespace JobExecutor.Api.Contracts;

/// <summary>
/// The state of a single active job.
/// </summary>
/// <typeparam name="TOut">The state model of the job.</typeparam>
public sealed record JobStateResponse<TOut>(string JobId, TOut State);

/// <summary>
/// The states of the active jobs.
/// </summary>
/// <param name="TotalCount">The total number of the active jobs of the job type.</param>
/// <param name="Count">The number of the jobs in this response.</param>
/// <param name="Jobs">The states of the jobs.</param>
/// <typeparam name="TOut">The state model of the job.</typeparam>
public sealed record ActiveJobsResponse<TOut>(int TotalCount, int Count, IReadOnlyList<JobStateResponse<TOut>> Jobs);
