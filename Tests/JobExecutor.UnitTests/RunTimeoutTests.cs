using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.UnitTests.Fixtures;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobExecutor.UnitTests;

/// <summary>
/// The timeout in the middle of the execution: the job has started, but it did not finish in time.
/// The client is rejected and the job must be stopped.
/// </summary>
public class RunTimeoutTests
{
    [Fact]
    public async Task RunJobAsync_ShouldCancelRunningJob_WhenTimeoutHappensInTheMiddleOfTheExecution()
    {
        using var fixture = new BackgroundJobsFixture<TestCountingJobInput, TestCountingJobResult, TestCountingJob>(
            o => o.Timeout = TimeSpan.FromMilliseconds(300));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCountingJobInput, TestCountingJobResult>>();

        var result = await manager.RunJobAsync(new JobId("job-1"), new TestCountingJobInput());

        // The client is rejected.
        Assert.False(result.Success);
        Assert.Equal("Timeout.", result.Result);

        // The job was cancelled, not left running.
        await WaitUntilAsync(() => fixture.Tracker.CountOf(TestCountingJob.CancelledEvent) == 1);
        Assert.Equal(0, fixture.Tracker.CountOf(TestCountingJob.CompletedEvent));
    }

    [Fact]
    public async Task RunJobAsync_ShouldStopTheWorkOfTheJob_WhenTimeoutHappensInTheMiddleOfTheExecution()
    {
        using var fixture = new BackgroundJobsFixture<TestCountingJobInput, TestCountingJobResult, TestCountingJob>(
            o => o.Timeout = TimeSpan.FromMilliseconds(300));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCountingJobInput, TestCountingJobResult>>();
        var jobId = new JobId("job-1");

        await manager.RunJobAsync(jobId, new TestCountingJobInput());

        await WaitUntilAsync(() => fixture.Tracker.CountOf(TestCountingJob.CancelledEvent) == 1);

        // The job is removed from the registry.
        await WaitUntilAsync(() => !manager.GetAllJobsAsync().GetAwaiter().GetResult().Jobs.ContainsKey(jobId));

        // The work of the job is really stopped: the number of the steps does not grow anymore.
        var steps = StepsOf(fixture);
        await Task.Delay(300);

        Assert.Equal(steps, StepsOf(fixture));
    }

    [Fact]
    public async Task RunJobAsync_ShouldShowTheProgressOfTheJob_WhileItIsRunning()
    {
        using var fixture = new BackgroundJobsFixture<TestCountingJobInput, TestCountingJobResult, TestCountingJob>(
            o => o.Timeout = TimeSpan.FromSeconds(30));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCountingJobInput, TestCountingJobResult>>();
        var jobId = new JobId("job-1");

        var runTask = manager.RunJobAsync(jobId, new TestCountingJobInput { StepDelayMs = 50 });

        // The job is running and its progress is visible.
        await WaitUntilAsync(() => fixture.Tracker.CountOf(TestCountingJob.StartedEvent) == 1);
        await WaitUntilAsync(() => GetSteps(manager, jobId) >= 2);

        Assert.Equal(1, (await manager.GetAllJobsAsync()).TotalCount);

        // The job is stopped, so the run does not wait for it.
        await manager.StopJobAsync(jobId);
        var result = await runTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(result.Success);
        Assert.Equal("cancelled", result.Result);
    }

    [Fact]
    public async Task RunJobAsync_ShouldCompleteTheJob_WhenItFinishesBeforeTheTimeout()
    {
        using var fixture = new BackgroundJobsFixture<TestCountingJobInput, TestCountingJobResult, TestCountingJob>(
            o => o.Timeout = TimeSpan.FromSeconds(30));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCountingJobInput, TestCountingJobResult>>();
        var jobId = new JobId("job-1");

        var result = await manager.RunJobAsync(jobId, new TestCountingJobInput { Steps = 3, StepDelayMs = 20 });

        Assert.True(result.Success);
        Assert.Equal(1, fixture.Tracker.CountOf(TestCountingJob.CompletedEvent));
        Assert.Equal(0, fixture.Tracker.CountOf(TestCountingJob.CancelledEvent));

        // The finished job is removed from the registry.
        await WaitUntilAsync(() => !manager.GetAllJobsAsync().GetAwaiter().GetResult().Jobs.ContainsKey(jobId));
    }

    /// <summary>
    /// Documents the limitation: the timeout cancels the token of the job, but it cannot stop a job
    /// that does not observe the token. Such a job keeps working until it finishes on its own.
    /// </summary>
    [Fact]
    public async Task RunJobAsync_ShouldNotStopTheJob_WhenTheJobIgnoresTheCancellation()
    {
        using var fixture = new BackgroundJobsFixture<TestCountingJobInput, TestCountingJobResult, TestCountingJob>(
            o => o.Timeout = TimeSpan.FromMilliseconds(300));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCountingJobInput, TestCountingJobResult>>();
        var jobId = new JobId("job-1");

        var result = await manager.RunJobAsync(jobId, new TestCountingJobInput { IgnoreCancellation = true });

        Assert.False(result.Success);
        Assert.Equal("Timeout.", result.Result);

        // The job ignores the cancellation and keeps working.
        await WaitUntilAsync(() => GetSteps(manager, jobId) >= 2);
        Assert.Equal(1, (await manager.GetAllJobsAsync()).TotalCount);
        Assert.Equal(0, fixture.Tracker.CountOf(TestCountingJob.CancelledEvent));
    }

    private static int GetSteps(
        IActiveJobManager<TestCountingJobInput, TestCountingJobResult> manager, JobId jobId)
    {
        var jobs = manager.GetAllJobsAsync().GetAwaiter().GetResult().Jobs;

        return jobs.TryGetValue(jobId, out var info) ? info.Result.Steps : 0;
    }

    private static int StepsOf(BackgroundJobsFixture<TestCountingJobInput, TestCountingJobResult, TestCountingJob> fixture)
        => int.Parse(fixture.Tracker.LastMessageOf(TestCountingJob.StepEvent));

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
