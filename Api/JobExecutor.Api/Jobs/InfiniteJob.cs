using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.Api.Jobs;

/// <summary>
/// A job that never finishes on its own. Only the execution timeout of the library stops it.
/// It shows that no job lives forever, and how long the caller waits before it gets the timeout.
/// </summary>
public sealed class InfiniteJob : IActiveJob<InfiniteJobInput, InfiniteJobState>
{
    private int _seconds;

    public async Task DoAsync(InfiniteJobInput input, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), token);

            _seconds++;
        }
    }

    public InfiniteJobState GetCurrentState() => new(_seconds);
}

/// <summary>
/// Input parameters of the infinite background job.
/// </summary>
public sealed record InfiniteJobInput
{
    /// <summary>
    /// The name of the job, used to distinguish several instances of it.
    /// </summary>
    public string Name { get; init; } = "infinite";
}

/// <summary>
/// The current state of the infinite background job.
/// </summary>
public sealed record InfiniteJobState(int Seconds);
