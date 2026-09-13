using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Queries;

namespace JobExecutor.Abstractions.Interfaces;

/// <summary>
/// Provides access to starting, running, stopping, and querying the state of background jobs.
/// </summary>
/// <typeparam name="TIn">Input parameters for the command that starts the background job.</typeparam>
/// <typeparam name="TOut">The current state of the background job.</typeparam>
public interface IJobManager<in TIn, TOut>
    where TIn : class
    where TOut : class
{
    /// <summary>
    /// Starts a background job without waiting for it to complete. If IJob.DoAsync throws,
    /// its execution is retried, which does not affect job start.
    /// </summary>
    /// <param name="input">Input parameters for running the job. On each retry after an error,
    /// IJob.DoAsync is restarted from the beginning with the same input model that was passed to this method.</param>
    /// <param name="maxNrOfRetries">Defaults to 5. The first attempt plus maxNrOfRetries retries,
    /// so IJob.DoAsync is invoked at most maxNrOfRetries + 1 times when exceptions occur.</param>
    /// <param name="minBackoff">Defaults to 1 second. Retries after an error start no earlier than minBackoff seconds.
    /// The actual delay is picked in the range from minBackoff to maxBackoff to spread the load evenly
    /// across the systems the job interacts with.</param>
    /// <param name="maxBackoff">Defaults to 3 seconds. Retries after an error start no later than maxBackoff seconds.
    /// The actual delay is picked in the range from minBackoff to maxBackoff to spread the load evenly
    /// across the systems the job interacts with.</param>
    /// <param name="jobId">Job identifier. Defaults to a normalized Guid.NewGuid(). Must be unique.</param>
    /// <param name="timeout">Defaults to 120 seconds. The time to wait for the start-job request.
    /// Expiration of this interval does NOT stop the job itself.</param>
    /// <returns>The result of starting the background job.</returns>
    Task<JobStartedResult> StartJobAsync(string jobId, TIn input,
        int? maxNrOfRetries = null, TimeSpan? minBackoff = null, TimeSpan? maxBackoff = null, TimeSpan? timeout = null);

    /// <summary>
    /// Runs a background job and waits for it to complete. If IJob.DoAsync throws, its execution is retried
    /// the specified number of times. Retries do not interrupt the wait, but once the retry count is exhausted,
    /// the result is returned.
    /// </summary>
    /// <param name="input">Input parameters for running the job. On each retry after an error,
    /// IJob.DoAsync is restarted from the beginning with the same input model that was passed to this method.</param>
    /// <param name="maxNrOfRetries">Defaults to 5. The first attempt plus maxNrOfRetries retries,
    /// so IJob.DoAsync is invoked at most maxNrOfRetries + 1 times when exceptions occur.</param>
    /// <param name="minBackoff">Defaults to 1 second. Retries after an error start no earlier than minBackoff seconds.
    /// The actual delay is picked in the range from minBackoff to maxBackoff to spread the load evenly
    /// across the systems the job interacts with.</param>
    /// <param name="maxBackoff">Defaults to 3 seconds. Retries after an error start no later than maxBackoff seconds.
    /// The actual delay is picked in the range from minBackoff to maxBackoff to spread the load evenly
    /// across the systems the job interacts with.</param>
    /// <param name="jobId">Job identifier. Defaults to a normalized Guid.NewGuid(). Must be unique.</param>
    /// <param name="timeout">Defaults to 120 seconds. The time to wait for the run-job request.
    /// Expiration of this interval does NOT stop the job itself.</param>
    /// <returns>The result of running the background job.</returns>
    Task<JobCompletedResult> RunJobAsync(string jobId, TIn input,
        int? maxNrOfRetries = null, TimeSpan? minBackoff = null, TimeSpan? maxBackoff = null, TimeSpan? timeout = null);

    /// <summary>
    /// Stops a background job.
    /// </summary>
    /// <param name="jobId">Identifier of the job to stop. It is normalized, so it can be passed as-is or already normalized.</param>
    /// <returns>The result of the stop attempt.</returns>
    Task<JobStoppedResult> StopJobAsync(string jobId);

    /// <summary>
    /// Gets the state of all running background jobs.
    /// </summary>
    /// <returns>Information about the background jobs.</returns>
    Task<JobsQueryResult<TOut>> GetAllJobsAsync();

    /// <summary>
    /// Gets the state of running background jobs page by page.
    /// </summary>
    /// <param name="skip">The number of jobs to skip.</param>
    /// <param name="take">The number of jobs to return.</param>
    /// <returns>Information about the background jobs.</returns>
    Task<JobsQueryResult<TOut>> GetJobsPageAsync(int skip, int take);

    /// <summary>
    /// Gets the state of running background jobs by a list of identifiers.
    /// </summary>
    /// <param name="jobIds">The list of job identifiers.</param>
    /// <returns>Information about the background jobs.</returns>
    Task<JobsQueryResult<TOut>> GetJobsByIdsAsync(ICollection<string> jobIds);
}
