using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.UnitTests.Fixtures;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobExecutor.UnitTests;

/// <summary>
/// The engines read the requests from the channel in a loop. A cancelled or a failed job must not
/// break that loop — the next requests must still be processed.
/// </summary>
public class EngineTests
{
    [Fact]
    public async Task RunJobEngine_ShouldProcessNextRequest_WhenRunningJobIsCancelled()
    {
        using var fixture = new BackgroundJobsFixture<TestCancellableJobInput, TestCancellableJobResult, TestCancellableJob>(
            o => o.ExecutionTimeout = TimeSpan.FromSeconds(10));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult>>();

        // The first job runs until it is cancelled.
        var first = manager.RunJobAsync(new JobId("first"), new TestCancellableJobInput { BlockUntilCancelled = true });
        await WaitUntilJobIsRunningAsync(manager, new JobId("first"));

        // The second job is queued behind the first one.
        var second = manager.RunJobAsync(new JobId("second"), new TestCancellableJobInput { BlockUntilCancelled = true });
        await WaitUntilJobIsRunningAsync(manager, new JobId("second"));

        var stopResult = await manager.StopJobAsync(new JobId("first"));
        Assert.True(stopResult.Success);

        var firstResult = await first.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(firstResult.Success);
        Assert.Equal("cancelled", firstResult.Result);

        // The engine survived the cancellation and processes the next request.
        var third = await manager.RunJobAsync(new JobId("third"), new TestCancellableJobInput())
            .WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(third.Success);

        // The queued job was not affected by the cancellation of the first one.
        Assert.True((await manager.GetAllJobsAsync()).Jobs.ContainsKey(new JobId("second")));

        await manager.StopJobAsync(new JobId("second"));
    }

    [Fact]
    public async Task StartJobEngine_ShouldProcessNextRequest_WhenRunningJobIsCancelled()
    {
        using var fixture = new BackgroundJobsFixture<TestCancellableJobInput, TestCancellableJobResult, TestCancellableJob>(
            o => o.ExecutionTimeout = TimeSpan.FromSeconds(10));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult>>();

        var first = manager.StartJobAsync(new JobId("first"), new TestCancellableJobInput { BlockUntilCancelled = true });
        Assert.True((await first).Success);
        await WaitUntilJobIsRunningAsync(manager, new JobId("first"));

        var second = manager.StartJobAsync(new JobId("second"), new TestCancellableJobInput { BlockUntilCancelled = true });
        Assert.True((await second).Success);
        await WaitUntilJobIsRunningAsync(manager, new JobId("second"));

        var stopResult = await manager.StopJobAsync(new JobId("first"));
        Assert.True(stopResult.Success);

        // The engine survived the cancellation and processes the next request.
        var third = await manager.StartJobAsync(new JobId("third"), new TestCancellableJobInput())
            .WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(third.Success);
        Assert.True((await manager.GetAllJobsAsync()).Jobs.ContainsKey(new JobId("second")));

        await manager.StopJobAsync(new JobId("second"));
    }

    [Fact]
    public async Task RunJobEngine_ShouldProcessNextRequest_WhenJobThrows()
    {
        using var fixture = new BackgroundJobsFixture<TestCancellableJobInput, TestCancellableJobResult, TestCancellableJob>(
            o => o.ExecutionTimeout = TimeSpan.FromSeconds(10));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult>>();

        var failed = await manager.RunJobAsync(new JobId("failed"), new TestCancellableJobInput { Throw = true });

        Assert.False(failed.Success);
        Assert.Equal("Test exception.", failed.Result);

        // The engine survived the failure and processes the next request.
        var next = await manager.RunJobAsync(new JobId("next"), new TestCancellableJobInput())
            .WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(next.Success);
    }

    [Fact]
    public async Task RunJobEngine_ShouldProcessNextRequest_WhenJobThrowsOperationCanceledExceptionOnItsOwn()
    {
        using var fixture = new BackgroundJobsFixture<TestCancellableJobInput, TestCancellableJobResult, TestCancellableJob>(
            o => o.ExecutionTimeout = TimeSpan.FromSeconds(10));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult>>();

        // The job throws OperationCanceledException, but its token was not cancelled.
        var failed = await manager.RunJobAsync(new JobId("failed"),
            new TestCancellableJobInput { ThrowOperationCanceledException = true });

        Assert.False(failed.Success);

        // The engine survived it and processes the next request.
        var next = await manager.RunJobAsync(new JobId("next"), new TestCancellableJobInput())
            .WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(next.Success);
    }

    /// <summary>
    /// Documents the current behaviour: a request that was submitted after the host stopped is never
    /// processed — the caller waits for the whole timeout to learn about it.
    /// </summary>
    [Fact]
    public async Task RunJobEngine_ShouldNotProcessRequest_WhenHostIsStopped()
    {
        var fixture = new BackgroundJobsFixture<TestCancellableJobInput, TestCancellableJobResult, TestCancellableJob>(
            o => o.StartTimeout = TimeSpan.FromMilliseconds(300));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult>>();

        // The host stops: the engines are stopped and nobody reads the channel anymore.
        fixture.Dispose();

        var result = await manager.RunJobAsync(new JobId("after-stop"), new TestCancellableJobInput());

        Assert.False(result.Success);
        Assert.Equal("Timeout.", result.Result);
    }

    private static async Task WaitUntilJobIsRunningAsync(
        IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult> manager, JobId jobId)
    {
        for (var i = 0; i < 100; i++)
        {
            if ((await manager.GetAllJobsAsync()).Jobs.ContainsKey(jobId))
                return;

            await Task.Delay(10);
        }
    }
}
