using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.Api.Jobs;

/// <summary>
/// A job that blocks the thread of the engine until it is cancelled. The engine reads the next
/// request only after the current job returns, so this job shows what happens to the requests
/// that wait in the queue behind it. The blocking is deliberate: it is the only way to occupy
/// the engine, because a truly asynchronous job does not block it.
/// </summary>
public sealed class BlockingJob : IActiveJob<BlockingJobInput, BlockingJobState>
{
    public Task DoAsync(BlockingJobInput input, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
            Thread.Sleep(input.PollMs);

        token.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    public BlockingJobState GetCurrentState() => new(0);
}

/// <summary>
/// Input parameters of the blocking background job.
/// </summary>
public sealed record BlockingJobInput
{
    /// <summary>
    /// The interval between the checks of the cancellation, in milliseconds.
    /// </summary>
    public int PollMs { get; init; } = 50;
}

/// <summary>
/// The current state of the blocking background job.
/// </summary>
public sealed record BlockingJobState(int Data);
