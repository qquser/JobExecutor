using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.Api.Jobs;

/// <summary>
/// A job that publishes its state as one immutable snapshot. The snapshot is written atomically,
/// so a reader never sees the fields of different steps. It is the correct pattern for the live
/// state of a job, in contrast to the mutating state job.
/// </summary>
public sealed class SnapshotJob : IActiveJob<SnapshotJobInput, SnapshotJobState>
{
    private SnapshotJobState _snapshot = new(0, 0, "step-0");

    public async Task DoAsync(SnapshotJobInput input, CancellationToken token)
    {
        for (var step = 1; step <= input.Steps; step++)
        {
            await Task.Delay(input.StepDelayMs, token);

            Volatile.Write(ref _snapshot, new SnapshotJobState(step, step * 2, $"step-{step}"));
        }
    }

    public SnapshotJobState GetCurrentState() => Volatile.Read(ref _snapshot);
}

/// <summary>
/// Input parameters of the snapshot background job.
/// </summary>
public sealed record SnapshotJobInput
{
    /// <summary>
    /// The number of steps the job performs.
    /// </summary>
    public int Steps { get; init; } = 20;

    /// <summary>
    /// The delay between the steps, in milliseconds.
    /// </summary>
    public int StepDelayMs { get; init; } = 100;
}

/// <summary>
/// The current state of the snapshot background job.
/// </summary>
public sealed record SnapshotJobState(int CompletedSteps, int DoubledSteps, string Label);
