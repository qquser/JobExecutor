using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.UnitTests.Fixtures;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobExecutor.UnitTests;

/// <summary>
/// The semantics of the cancellation token of the manager: the token cancels the job and the wait
/// of the caller.
/// </summary>
public class CancellationTokenTests
{
    [Fact]
    public async Task RunJobAsync_ShouldThrowOperationCanceledException_WhenTokenIsCancelled()
    {
        using var fixture = new BackgroundJobsFixture<TestCancellableJobInput, TestCancellableJobResult, TestCancellableJob>(
            o => o.Timeout = TimeSpan.FromSeconds(10));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult>>();
        using var cts = new CancellationTokenSource();

        var runTask = manager.RunJobAsync(new JobId("job-1"), new TestCancellableJobInput { BlockUntilCancelled = true }, cts.Token);
        await WaitUntilJobIsRunningAsync(manager, new JobId("job-1"));

        await cts.CancelAsync();

        // The wait of the caller is interrupted by the token.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
    }

    [Fact]
    public async Task RunJobAsync_ShouldCancelRunningJob_WhenTokenIsCancelled()
    {
        using var fixture = new BackgroundJobsFixture<TestCancellableJobInput, TestCancellableJobResult, TestCancellableJob>(
            o => o.Timeout = TimeSpan.FromSeconds(10));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult>>();
        using var cts = new CancellationTokenSource();

        var runTask = manager.RunJobAsync(new JobId("job-1"), new TestCancellableJobInput { BlockUntilCancelled = true }, cts.Token);
        await WaitUntilJobIsRunningAsync(manager, new JobId("job-1"));

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);

        // The job itself is cancelled and removed from the registry.
        await WaitUntilJobRemovedAsync(manager, new JobId("job-1"));
    }

    [Fact]
    public async Task StartJobAsync_ShouldCancelRunningJob_WhenTokenIsCancelled()
    {
        using var fixture = new BackgroundJobsFixture<TestCancellableJobInput, TestCancellableJobResult, TestCancellableJob>(
            o => o.Timeout = TimeSpan.FromSeconds(10));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult>>();
        using var cts = new CancellationTokenSource();

        var startResult = await manager.StartJobAsync(
            new JobId("job-1"),
            new TestCancellableJobInput { BlockUntilCancelled = true },
            cts.Token);

        // The start call returns as soon as the job is registered, the token does not interrupt it.
        Assert.True(startResult.Success);
        await WaitUntilJobIsRunningAsync(manager, new JobId("job-1"));

        await cts.CancelAsync();

        // The job itself is cancelled and removed from the registry.
        await WaitUntilJobRemovedAsync(manager, new JobId("job-1"));
    }

    [Fact]
    public async Task RunJobAsync_ShouldReturnCancelled_WhenTokenIsAlreadyCancelled()
    {
        using var fixture = new BackgroundJobsFixture<TestCancellableJobInput, TestCancellableJobResult, TestCancellableJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult>>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await manager.RunJobAsync(new JobId("job-1"), new TestCancellableJobInput(), cts.Token);

        // The token was cancelled before the call, so the job is not even started.
        Assert.False(result.Success);
        Assert.Equal("cancelled", result.Result);
        Assert.Equal(0, (await manager.GetAllJobsAsync()).TotalCount);
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

        Assert.Fail($"The job {jobId.Value} did not start in time.");
    }

    private static async Task WaitUntilJobRemovedAsync(
        IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult> manager, JobId jobId)
    {
        for (var i = 0; i < 100; i++)
        {
            if (!(await manager.GetAllJobsAsync()).Jobs.ContainsKey(jobId))
                return;

            await Task.Delay(10);
        }

        Assert.Fail($"The job {jobId.Value} was not removed in time.");
    }
}
