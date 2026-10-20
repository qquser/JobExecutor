using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.UnitTests.Fixtures;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobExecutor.UnitTests;

/// <summary>
/// The timeout of the start: the client stops waiting for the job to start. The job must not start
/// after the client was rejected.
/// </summary>
public class StartTimeoutTests
{
    [Fact]
    public async Task StartJobAsync_ShouldNotStartJob_WhenTimeoutHappensWhileTheJobIsBeingResolved()
    {
        // The job takes 300 milliseconds to be created, the client waits only 50 milliseconds.
        using var fixture = new BackgroundJobsFixture<TestSlowStartJobInput, TestSlowStartJobResult, TestSlowStartJob>(
            o => o.Timeout = TimeSpan.FromMilliseconds(50));
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
            o => o.Timeout = TimeSpan.FromSeconds(10));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestSlowStartJobInput, TestSlowStartJobResult>>();

        var result = await manager.StartJobAsync(new JobId("slow"), new TestSlowStartJobInput());

        Assert.True(result.Success);
        Assert.Equal(1, fixture.Tracker.CountOf(TestSlowStartJob.StartedEvent));
    }
}
