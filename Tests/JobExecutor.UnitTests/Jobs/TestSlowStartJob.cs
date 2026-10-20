using JobExecutor.Abstractions.Interfaces;
using JobExecutor.UnitTests.Fixtures;

namespace JobExecutor.UnitTests.Jobs;

/// <summary>
/// The job that takes a long time to be created. It opens the window between the cancellation
/// check of the engine and the start of the job.
/// </summary>
public sealed class TestSlowStartJob : IActiveJob<TestSlowStartJobInput, TestSlowStartJobResult>
{
    public const string StartedEvent = "slow-job-started";

    private readonly JobEventTracker _tracker;

    public TestSlowStartJob(JobEventTracker tracker)
    {
        Thread.Sleep(300);
        _tracker = tracker;
    }

    public Task DoAsync(TestSlowStartJobInput input, CancellationToken token)
    {
        _tracker.Record(StartedEvent);
        token.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    public TestSlowStartJobResult GetCurrentState() => new(0);
}

public sealed record TestSlowStartJobInput;

public sealed record TestSlowStartJobResult(int Data);
