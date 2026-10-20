namespace JobExecutor.Abstractions.Models.Options;

/// <summary>
/// The timeouts of a background job. The two timeouts are separate on purpose: the first one
/// limits the wait of the caller for the start of the job, the second one limits the life of the
/// job itself.
/// </summary>
/// <remarks>
/// The values are set explicitly at the registration via <c>AddBackgroundJobs</c>, so the user
/// always sees which timeout is the wait for the start and which one is the whole life of the job.
/// </remarks>
public sealed record JobTimeoutOptions
{
    /// <summary>
    /// How long the caller waits for the request to be queued and the job to be started.
    /// When it expires, the wait is cancelled and the job does not start.
    /// The default is 2 minutes.
    /// </summary>
    public TimeSpan StartTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// How long the job may run after it has started. The timeout is counted from the start of the
    /// execution, so the job is stopped when it lives longer than this value.
    /// The default is 30 minutes.
    /// </summary>
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromMinutes(30);
}
