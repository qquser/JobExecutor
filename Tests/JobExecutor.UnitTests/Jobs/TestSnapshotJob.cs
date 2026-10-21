using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

/// <summary>
/// The job that publishes its state as one immutable snapshot. The snapshot is written atomically,
/// so a reader never sees the fields of different steps.
/// </summary>
public sealed class TestSnapshotJob : IActiveJob<TestSnapshotJobInput, TestSnapshotJobResult>
{
    private TestSnapshotJobResult _snapshot = new(0, 0, "step-0");

    public async Task DoAsync(TestSnapshotJobInput input, CancellationToken token)
    {
        for (var step = 1; step <= input.Steps; step++)
        {
            await Task.Delay(input.GapMs, token);

            // The snapshot is published atomically, so a reader never sees a mixture of the steps.
            Volatile.Write(ref _snapshot, new TestSnapshotJobResult(step, step * 2, $"step-{step}"));
        }
    }

    public TestSnapshotJobResult GetCurrentState() => Volatile.Read(ref _snapshot);
}

public sealed record TestSnapshotJobInput
{
    public int Steps { get; init; } = 100;

    /// <summary>
    /// The pause between the changes of the state.
    /// </summary>
    public int GapMs { get; init; } = 5;
}

public sealed record TestSnapshotJobResult(int CompletedSteps, int DoubledSteps, string Label);
