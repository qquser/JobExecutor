using JobExecutor.Abstractions.Interfaces;
using JobExecutor.UnitTests.Fixtures;

namespace JobExecutor.UnitTests.Jobs;

/// <summary>
/// The job that works in steps until it is finished or cancelled. It reports its progress through
/// the tracker, so the tests can see what happened to it.
/// </summary>
public sealed class TestCountingJob : IActiveJob<TestCountingJobInput, TestCountingJobResult>
{
    public const string StartedEvent = "counting-started";
    public const string CancelledEvent = "counting-cancelled";
    public const string CompletedEvent = "counting-completed";
    public const string StepEvent = "counting-step";

    private readonly JobEventTracker _tracker;
    private int _steps;

    public TestCountingJob(JobEventTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task DoAsync(TestCountingJobInput input, CancellationToken token)
    {
        _tracker.Record(StartedEvent);

        try
        {
            // Zero steps means that the job works until it is cancelled.
            var steps = input.Steps == 0 ? input.MaxSteps : input.Steps;

            for (var step = 0; step < steps; step++)
            {
                await Task.Delay(input.StepDelayMs, input.IgnoreCancellation ? CancellationToken.None : token);
                _steps++;
                _tracker.Record(StepEvent, _steps.ToString());
            }

            _tracker.Record(CompletedEvent);
        }
        catch (OperationCanceledException)
        {
            _tracker.Record(CancelledEvent);
            throw;
        }
    }

    public TestCountingJobResult GetCurrentState() => new(_steps);
}

public sealed record TestCountingJobInput
{
    /// <summary>
    /// The number of the steps. Zero means that the job works until it is cancelled.
    /// </summary>
    public int Steps { get; init; }

    public int StepDelayMs { get; init; } = 50;

    /// <summary>
    /// The bound of the endless work, so the job always finishes.
    /// </summary>
    public int MaxSteps { get; init; } = 200;

    /// <summary>
    /// Ignores the cancellation of the token, so the job cannot be stopped.
    /// </summary>
    public bool IgnoreCancellation { get; init; }
}

public sealed record TestCountingJobResult(int Steps);
