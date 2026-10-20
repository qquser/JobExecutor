using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.UnitTests.Fixtures;

namespace JobExecutor.UnitTests.Jobs;

/// <summary>
/// The job that calls another job through the manager of the other job.
/// </summary>
public sealed class TestCallerJob : IActiveJob<TestCallerJobInput, TestCallerJobResult>
{
    public const string StartedEvent = "caller-started";
    public const string NestedSucceededEvent = "nested-succeeded";
    public const string NestedFailedEvent = "nested-failed";

    private readonly IActiveJobManager<TestTargetJobInput, TestTargetJobResult> _targetManager;
    private readonly JobEventTracker _tracker;

    public TestCallerJob(
        IActiveJobManager<TestTargetJobInput, TestTargetJobResult> targetManager,
        JobEventTracker tracker)
    {
        _targetManager = targetManager;
        _tracker = tracker;
    }

    public async Task DoAsync(TestCallerJobInput input, CancellationToken token)
    {
        _tracker.Record(StartedEvent);

        var targetInput = new TestTargetJobInput
        {
            Steps = input.Steps,
            StepDelayMs = input.StepDelayMs,
            Throw = input.TargetThrows,
        };

        JobCompletedResult? nestedResult = null;

        if (input.Mode == TestCallerMode.Run)
        {
            nestedResult = input.PassToken
                ? await _targetManager.RunJobAsync(new JobId(input.TargetJobId), targetInput, token)
                : await _targetManager.RunJobAsync(new JobId(input.TargetJobId), targetInput);
        }
        else
        {
            var startResult = input.PassToken
                ? await _targetManager.StartJobAsync(new JobId(input.TargetJobId), targetInput, token)
                : await _targetManager.StartJobAsync(new JobId(input.TargetJobId), targetInput);

            nestedResult = new JobCompletedResult(startResult.Success, startResult.Result);
        }

        if (nestedResult.Success)
            _tracker.Record(NestedSucceededEvent);
        else
            _tracker.Record(NestedFailedEvent, nestedResult.Result);

        if (input.CallerDelayMs > 0)
            await Task.Delay(input.CallerDelayMs, token);

        if (input.WaitForCancellation)
            await Task.Delay(Timeout.Infinite, token);
    }

    public TestCallerJobResult GetCurrentState() => new(0);
}

public enum TestCallerMode
{
    Start,
    Run,
}

public sealed record TestCallerJobInput
{
    public string TargetJobId { get; init; } = "nested";

    public TestCallerMode Mode { get; init; } = TestCallerMode.Run;

    public int Steps { get; init; } = 1;

    public int StepDelayMs { get; init; } = 10;

    public bool TargetThrows { get; init; }

    /// <summary>
    /// Passes the token of the caller to the nested job, so the nested job is cancelled together
    /// with the caller.
    /// </summary>
    public bool PassToken { get; init; } = true;

    public int CallerDelayMs { get; init; }

    public bool WaitForCancellation { get; init; }
}

public sealed record TestCallerJobResult(int CompletedSteps);
