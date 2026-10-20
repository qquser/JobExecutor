using System.Diagnostics;
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.UnitTests.Fixtures;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobExecutor.UnitTests;

/// <summary>
/// The behaviour of a job that calls another job (a job inside a job).
/// </summary>
public class NestedJobsTests
{
    [Fact]
    public async Task RunJobAsync_ShouldRunNestedJob_WhenJobCallsAnotherJob()
    {
        using var fixture = new CompositeJobsFixture();
        var callerManager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCallerJobInput, TestCallerJobResult>>();

        var result = await callerManager.RunJobAsync(
            new JobId("caller-1"),
            new TestCallerJobInput { TargetJobId = "target-1", Mode = TestCallerMode.Run, Steps = 2, StepDelayMs = 10 });

        Assert.True(result.Success);
        Assert.Equal(1, fixture.Tracker.CountOf(TestCallerJob.NestedSucceededEvent));
        Assert.Equal(1, fixture.Tracker.CountOf(TestTargetJob.CompletedEvent));
    }

    [Fact]
    public async Task StartJobAsync_ShouldStartNestedJob_WhenJobCallsAnotherJob()
    {
        using var fixture = new CompositeJobsFixture();
        var callerManager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCallerJobInput, TestCallerJobResult>>();
        var targetManager = fixture.Provider.GetRequiredService<IActiveJobManager<TestTargetJobInput, TestTargetJobResult>>();

        var result = await callerManager.StartJobAsync(
            new JobId("caller-1"),
            new TestCallerJobInput { TargetJobId = "target-1", Mode = TestCallerMode.Start, Steps = 100, StepDelayMs = 20 });

        Assert.True(result.Success);

        // The start call returns before the body of the job runs, so the nested call is awaited.
        await WaitUntilAsync(() => fixture.Tracker.CountOf(TestCallerJob.NestedSucceededEvent) == 1);
        Assert.Equal(1, fixture.Tracker.CountOf(TestCallerJob.NestedSucceededEvent));

        // The nested job lives in the registry of its own type and is visible there.
        await WaitUntilAsync(() => targetManager.GetAllJobsAsync().GetAwaiter().GetResult().Jobs.ContainsKey(new JobId("target-1")));

        var targetJobs = await targetManager.GetAllJobsAsync();
        Assert.True(targetJobs.Jobs.ContainsKey(new JobId("target-1")));

        // The caller has finished and is gone from its own registry.
        await WaitUntilAsync(() => callerManager.GetAllJobsAsync().GetAwaiter().GetResult().TotalCount == 0);
    }

    [Fact]
    public async Task RunJobAsync_ShouldReturnNestedFailureToCaller_WhenNestedJobThrows()
    {
        using var fixture = new CompositeJobsFixture();
        var callerManager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCallerJobInput, TestCallerJobResult>>();

        var result = await callerManager.RunJobAsync(
            new JobId("caller-1"),
            new TestCallerJobInput { TargetJobId = "target-1", Mode = TestCallerMode.Run, TargetThrows = true });

        // The caller handles the failure of the nested job itself, so the caller succeeds.
        Assert.True(result.Success);
        Assert.Equal(1, fixture.Tracker.CountOf(TestCallerJob.NestedFailedEvent));
        Assert.Equal("Nested failure.", fixture.Tracker.LastMessageOf(TestCallerJob.NestedFailedEvent));
    }

    /// <summary>
    /// The token of the caller is passed to the nested job, so the nested job is cancelled together
    /// with the parent job.
    /// </summary>
    [Fact]
    public async Task StopJobAsync_ShouldCancelNestedJob_WhenParentJobPassesItsToken()
    {
        using var fixture = new CompositeJobsFixture();
        var callerManager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCallerJobInput, TestCallerJobResult>>();
        var targetManager = fixture.Provider.GetRequiredService<IActiveJobManager<TestTargetJobInput, TestTargetJobResult>>();
        var callerJobId = new JobId("caller-1");

        var startResult = await callerManager.StartJobAsync(
            callerJobId,
            new TestCallerJobInput
            {
                TargetJobId = "target-1",
                Mode = TestCallerMode.Start,
                Steps = 1000,
                StepDelayMs = 50,
                WaitForCancellation = true,
                PassToken = true,
            });

        Assert.True(startResult.Success);
        await WaitUntilAsync(() => fixture.Tracker.CountOf(TestTargetJob.StartedEvent) == 1);

        var stopResult = await callerManager.StopJobAsync(callerJobId);

        Assert.True(stopResult.Success);

        // The parent job is gone.
        await WaitUntilAsync(() => !callerManager.GetAllJobsAsync().GetAwaiter().GetResult().Jobs.ContainsKey(callerJobId));

        // The nested job is cancelled together with its parent.
        await WaitUntilAsync(() => fixture.Tracker.CountOf(TestTargetJob.CancelledEvent) == 1);
        await WaitUntilAsync(() => !targetManager.GetAllJobsAsync().GetAwaiter().GetResult().Jobs.ContainsKey(new JobId("target-1")));
    }

    /// <summary>
    /// Documents the current behaviour: the cancellation is propagated only when the caller passes
    /// its token to the nested job. Without the token the nested job keeps running.
    /// </summary>
    [Fact]
    public async Task StopJobAsync_ShouldNotCancelNestedJob_WhenParentJobDoesNotPassItsToken()
    {
        using var fixture = new CompositeJobsFixture();
        var callerManager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCallerJobInput, TestCallerJobResult>>();
        var targetManager = fixture.Provider.GetRequiredService<IActiveJobManager<TestTargetJobInput, TestTargetJobResult>>();
        var callerJobId = new JobId("caller-1");

        var startResult = await callerManager.StartJobAsync(
            callerJobId,
            new TestCallerJobInput
            {
                TargetJobId = "target-1",
                Mode = TestCallerMode.Start,
                Steps = 1000,
                StepDelayMs = 50,
                WaitForCancellation = true,
                PassToken = false,
            });

        Assert.True(startResult.Success);
        await WaitUntilAsync(() => fixture.Tracker.CountOf(TestTargetJob.StartedEvent) == 1);

        var stopResult = await callerManager.StopJobAsync(callerJobId);

        Assert.True(stopResult.Success);

        // The parent job is gone.
        await WaitUntilAsync(() => !callerManager.GetAllJobsAsync().GetAwaiter().GetResult().Jobs.ContainsKey(callerJobId));

        // The nested job is still running — it was not cancelled together with its parent.
        Assert.True((await targetManager.GetAllJobsAsync()).Jobs.ContainsKey(new JobId("target-1")));
        Assert.Equal(0, fixture.Tracker.CountOf(TestTargetJob.CancelledEvent));

        await targetManager.StopJobAsync(new JobId("target-1"));
    }

    /// <summary>
    /// The timeout of the parent job stops the nested job as well, when the caller passes its token.
    /// </summary>
    [Fact]
    public async Task RunJobAsync_ShouldCancelNestedJob_WhenParentJobTimesOut()
    {
        using var fixture = new CompositeJobsFixture(configureTimeout: o => o.Timeout = TimeSpan.FromMilliseconds(200));
        var callerManager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCallerJobInput, TestCallerJobResult>>();
        var targetManager = fixture.Provider.GetRequiredService<IActiveJobManager<TestTargetJobInput, TestTargetJobResult>>();

        var result = await callerManager.RunJobAsync(
            new JobId("caller-1"),
            new TestCallerJobInput
            {
                TargetJobId = "target-1",
                Mode = TestCallerMode.Start,
                Steps = 1000,
                StepDelayMs = 50,
                WaitForCancellation = true,
                PassToken = true,
            });

        Assert.False(result.Success);
        Assert.Equal("Timeout.", result.Result);

        await WaitUntilAsync(() => fixture.Tracker.CountOf(TestTargetJob.StartedEvent) == 1);

        // The timeout of the parent job stops the nested job as well.
        await WaitUntilAsync(() => fixture.Tracker.CountOf(TestTargetJob.CancelledEvent) == 1);
        await WaitUntilAsync(() => !targetManager.GetAllJobsAsync().GetAwaiter().GetResult().Jobs.ContainsKey(new JobId("target-1")));
    }

    [Fact]
    public async Task StartJobAsync_ShouldReturnExists_WhenJobCallsItselfWithSameJobId()
    {
        using var fixture = new CompositeJobsFixture();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestRecursiveJobInput, TestRecursiveJobResult>>();

        var result = await manager.StartJobAsync(
            new JobId("recursive"),
            new TestRecursiveJobInput { JobId = "recursive", Depth = 3, UseSameJobId = true });

        Assert.True(result.Success);

        // The duplicate guard of the registry stops the recursion.
        await WaitUntilAsync(() => fixture.Tracker.CountOf(TestRecursiveJob.FailedEvent) == 1);
        Assert.Equal("recursive job exists.", fixture.Tracker.LastMessageOf(TestRecursiveJob.FailedEvent));
    }

    [Fact]
    public async Task StartJobAsync_ShouldStartNestedChain_WhenJobCallsItselfWithDifferentJobIds()
    {
        using var fixture = new CompositeJobsFixture();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestRecursiveJobInput, TestRecursiveJobResult>>();

        var result = await manager.StartJobAsync(
            new JobId("recursive"),
            new TestRecursiveJobInput { JobId = "recursive", Depth = 3, UseSameJobId = false });

        Assert.True(result.Success);

        // The library has no depth limit — the chain is as long as the caller asks for it.
        await WaitUntilAsync(() => fixture.Tracker.CountOf(TestRecursiveJob.SucceededEvent) == 3);
        Assert.Equal(4, fixture.Tracker.CountOf(TestRecursiveJob.EnteredEvent));
        Assert.Equal(3, fixture.Tracker.CountOf(TestRecursiveJob.SucceededEvent));
    }

    /// <summary>
    /// Documents the current behaviour: the timeout is a global option, the timeout of the last
    /// registered job wins for every job.
    /// </summary>
    [Fact]
    public async Task RunJobAsync_ShouldUseTimeoutOfLastRegisteredJob_WhenJobsHaveDifferentTimeouts()
    {
        // The target is registered first with a long timeout, the caller is registered last with a short one.
        using var fixture = new CompositeJobsFixture(
            configureTimeout: o => o.Timeout = TimeSpan.FromMilliseconds(100),
            configureTargetTimeout: o => o.Timeout = TimeSpan.FromSeconds(5));
        var callerManager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCallerJobInput, TestCallerJobResult>>();

        var stopwatch = Stopwatch.StartNew();
        var result = await callerManager.RunJobAsync(
            new JobId("caller-1"),
            new TestCallerJobInput { Mode = TestCallerMode.Run, Steps = 1, StepDelayMs = 10, CallerDelayMs = 1000 });

        stopwatch.Stop();

        // The caller was registered last, so its timeout is applied to the target as well.
        Assert.False(result.Success);
        Assert.Equal("Timeout.", result.Result);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"The timeout of the target was applied, elapsed: {stopwatch.Elapsed}");
    }

    /// <summary>
    /// Documents the current behaviour: the timeout of an earlier registered job is overridden by the
    /// timeout of a later registered job.
    /// </summary>
    [Fact]
    public async Task RunJobAsync_ShouldIgnoreOwnTimeoutOfEarlierRegisteredJob_WhenJobsHaveDifferentTimeouts()
    {
        // The target is registered first with a short timeout, the caller is registered last with a long one.
        using var fixture = new CompositeJobsFixture(
            configureTimeout: o => o.Timeout = TimeSpan.FromSeconds(5),
            configureTargetTimeout: o => o.Timeout = TimeSpan.FromMilliseconds(50));
        var targetManager = fixture.Provider.GetRequiredService<IActiveJobManager<TestTargetJobInput, TestTargetJobResult>>();

        var stopwatch = Stopwatch.StartNew();
        var result = await targetManager.RunJobAsync(
            new JobId("target-1"),
            new TestTargetJobInput { Steps = 20, StepDelayMs = 10 });

        stopwatch.Stop();

        // The target was configured with 50 milliseconds, but the timeout of the caller won.
        Assert.True(result.Success);
        Assert.True(stopwatch.Elapsed > TimeSpan.FromMilliseconds(50), $"The own timeout of the target was applied, elapsed: {stopwatch.Elapsed}");
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;

            await Task.Delay(10);
        }

        Assert.True(condition(), "The expected state was not reached within the timeout.");
    }
}
