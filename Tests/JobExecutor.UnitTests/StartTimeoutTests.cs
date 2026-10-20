using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.UnitTests.Fixtures;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobExecutor.UnitTests;

/// <summary>
/// The start timeout: the client stops waiting for the job to start. The job must not start
/// after the client was rejected.
/// </summary>
public class StartTimeoutTests
{
    [Fact]
    public async Task StartJobAsync_ShouldNotStartJob_WhenTimeoutHappensWhileTheJobIsBeingResolved()
    {
        // The job takes 300 milliseconds to be created, the client waits only 50 milliseconds.
        using var fixture = new BackgroundJobsFixture<TestSlowStartJobInput, TestSlowStartJobResult, TestSlowStartJob>(
            o => o.StartTimeout = TimeSpan.FromMilliseconds(50));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestSlowStartJobInput, TestSlowStartJobResult>>();

        var result = await manager.StartJobAsync(new JobId("slow"), new TestSlowStartJobInput());

        // The client was rejected.
        Assert.False(result.Success);
        Assert.Equal("Timeout.", result.Result);

        // The job must not start after the client was rejected.
        await Task.Delay(600);

        Assert.Equal(0, fixture.Tracker.CountOf(TestSlowStartJob.StartedEvent));
        Assert.Equal(0, (await manager.GetAllJobsAsync()).TotalCount);
    }

    [Fact]
    public async Task StartJobAsync_ShouldStartJob_WhenTheJobIsResolvedBeforeTheTimeout()
    {
        using var fixture = new BackgroundJobsFixture<TestSlowStartJobInput, TestSlowStartJobResult, TestSlowStartJob>(
            o => o.StartTimeout = TimeSpan.FromSeconds(10));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestSlowStartJobInput, TestSlowStartJobResult>>();

        var result = await manager.StartJobAsync(new JobId("slow"), new TestSlowStartJobInput());

        Assert.True(result.Success);
        Assert.Equal(1, fixture.Tracker.CountOf(TestSlowStartJob.StartedEvent));
    }

    /// <summary>
    /// The start timeout limits the wait of the caller for the start of the job only. The job itself
    /// lives as long as the execution timeout allows it.
    /// </summary>
    [Fact]
    public async Task StartJobAsync_ShouldNotStopTheJob_WhenItRunsLongerThanTheStartTimeout()
    {
        using var fixture = new BackgroundJobsFixture<TestCountingJobInput, TestCountingJobResult, TestCountingJob>(
            o =>
            {
                o.StartTimeout = TimeSpan.FromMilliseconds(100);
                o.ExecutionTimeout = TimeSpan.FromSeconds(30);
            });
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCountingJobInput, TestCountingJobResult>>();
        var jobId = new JobId("job-1");

        var result = await manager.StartJobAsync(jobId, new TestCountingJobInput { Steps = 20, StepDelayMs = 50 });

        // The job started in time, so the call succeeded.
        Assert.True(result.Success);

        // The job keeps running: the start timeout does not limit the life of the job.
        await WaitUntilAsync(() => fixture.Tracker.CountOf(TestCountingJob.StepEvent) > 0);
        await Task.Delay(300);

        Assert.Equal(0, fixture.Tracker.CountOf(TestCountingJob.CancelledEvent));
        Assert.True((await manager.GetAllJobsAsync()).Jobs.ContainsKey(jobId));

        await manager.StopJobAsync(jobId);
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
