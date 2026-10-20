using JobExecutor.Abstractions.Interfaces;
using JobExecutor.UnitTests.Fixtures;

namespace JobExecutor.UnitTests.Jobs;

/// <summary>
/// The job that is called by another job. It reports its lifecycle through the tracker.
/// </summary>
public sealed class TestTargetJob : IActiveJob<TestTargetJobInput, TestTargetJobResult>
{
    public const string StartedEvent = "target-started";
    public const string CompletedEvent = "target-completed";
    public const string CancelledEvent = "target-cancelled";

    private readonly JobEventTracker _tracker;
    private int _completedSteps;

    public TestTargetJob(JobEventTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task DoAsync(TestTargetJobInput input, CancellationToken token)
    {
        _tracker.Record(StartedEvent);

        try
        {
            for (var step = 0; step < input.Steps; step++)
            {
                await Task.Delay(input.StepDelayMs, token);
                _completedSteps = step + 1;
            }

            _tracker.Record(CompletedEvent);
        }
        catch (OperationCanceledException)
        {
            _tracker.Record(CancelledEvent);
            throw;
        }

        if (input.Throw)
            throw new InvalidOperationException("Nested failure.");
    }

    public TestTargetJobResult GetCurrentState() => new(_completedSteps);
}

public sealed record TestTargetJobInput
{
    public int Steps { get; init; } = 1;

    public int StepDelayMs { get; init; } = 10;

    public bool Throw { get; init; }
}

public sealed record TestTargetJobResult(int CompletedSteps);
