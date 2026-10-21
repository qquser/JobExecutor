using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

/// <summary>
/// The job that changes the fields of its state one by one, with a pause between the changes.
/// The pause opens the window in which a reader of the state can see the fields of different
/// steps. It is the demonstration of a live state that is not a snapshot.
/// </summary>
public sealed class TestTornStateJob : IActiveJob<TestTornStateJobInput, TestTornStateJobResult>
{
    private int _completedSteps;
    private int _doubledSteps;
    private string _label = string.Empty;

    public async Task DoAsync(TestTornStateJobInput input, CancellationToken token)
    {
        for (var step = 1; step <= input.Steps; step++)
        {
            _completedSteps = step;

            // The pause between the changes opens the window for a reader that sees a mixture of
            // the steps of the job.
            await Task.Delay(input.GapMs, token);

            _doubledSteps = step * 2;

            await Task.Delay(input.GapMs, token);

            _label = $"step-{step}";
        }
    }

    public TestTornStateJobResult GetCurrentState() => new(_completedSteps, _doubledSteps, _label);
}

public sealed record TestTornStateJobInput
{
    public int Steps { get; init; } = 100;

    /// <summary>
    /// The pause between the changes of the fields of the state.
    /// </summary>
    public int GapMs { get; init; } = 5;
}

public sealed record TestTornStateJobResult(int CompletedSteps, int DoubledSteps, string Label);
