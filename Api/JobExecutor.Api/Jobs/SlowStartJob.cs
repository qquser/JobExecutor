using JobExecutor.Abstractions.Interfaces;
using Microsoft.Extensions.Options;

namespace JobExecutor.Api.Jobs;

/// <summary>
/// The options of the slow background job: how long the creation of the job takes.
/// </summary>
public sealed record SlowStartJobOptions
{
    /// <summary>
    /// The delay of the creation of the job. The delay happens while the job is being resolved,
    /// so it opens the window between the start of the job and the moment the caller learns that
    /// the job has started.
    /// </summary>
    public TimeSpan ResolutionDelay { get; set; } = TimeSpan.FromSeconds(2);
}

/// <summary>
/// A job that takes a long time to be created. The caller waits for the start of the job, so a
/// slow creation eats the start timeout. The delay is in the constructor on purpose: the job is
/// resolved by the container before it is registered and started.
/// </summary>
public sealed class SlowStartJob : IActiveJob<SlowStartJobInput, SlowStartJobState>
{
    private readonly SlowStartJobState _state;

    public SlowStartJob(IOptions<SlowStartJobOptions> options)
    {
        Thread.Sleep(options.Value.ResolutionDelay);

        _state = new SlowStartJobState(0);
    }

    public Task DoAsync(SlowStartJobInput input, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    public SlowStartJobState GetCurrentState() => _state;
}

/// <summary>
/// Input parameters of the slow background job.
/// </summary>
public sealed record SlowStartJobInput
{
    /// <summary>
    /// The number of steps the job performs.
    /// </summary>
    public int Steps { get; init; } = 1;
}

/// <summary>
/// The current state of the slow background job.
/// </summary>
public sealed record SlowStartJobState(int Data);
