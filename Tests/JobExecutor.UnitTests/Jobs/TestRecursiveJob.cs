using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.UnitTests.Fixtures;

namespace JobExecutor.UnitTests.Jobs;

/// <summary>
/// The job that calls itself through its own manager.
/// </summary>
public sealed class TestRecursiveJob : IActiveJob<TestRecursiveJobInput, TestRecursiveJobResult>
{
    public const string EnteredEvent = "recursive-entered";
    public const string SucceededEvent = "recursive-succeeded";
    public const string FailedEvent = "recursive-failed";

    private readonly IActiveJobManager<TestRecursiveJobInput, TestRecursiveJobResult> _selfManager;
    private readonly JobEventTracker _tracker;

    public TestRecursiveJob(
        IActiveJobManager<TestRecursiveJobInput, TestRecursiveJobResult> selfManager,
        JobEventTracker tracker)
    {
        _selfManager = selfManager;
        _tracker = tracker;
    }

    public async Task DoAsync(TestRecursiveJobInput input, CancellationToken token)
    {
        _tracker.Record(EnteredEvent);

        if (input.Depth <= 0)
            return;

        var childId = input.UseSameJobId ? input.JobId : $"{input.JobId}-child";
        var childInput = input with { JobId = childId, Depth = input.Depth - 1 };

        var result = await _selfManager.StartJobAsync(new JobId(childId), childInput);

        if (result.Success)
            _tracker.Record(SucceededEvent);
        else
            _tracker.Record(FailedEvent, result.Result);
    }

    public TestRecursiveJobResult GetCurrentState() => new(0);
}

public sealed record TestRecursiveJobInput
{
    public string JobId { get; init; } = "recursive";

    public int Depth { get; init; } = 3;

    public bool UseSameJobId { get; init; }
}

public sealed record TestRecursiveJobResult(int CompletedSteps);
