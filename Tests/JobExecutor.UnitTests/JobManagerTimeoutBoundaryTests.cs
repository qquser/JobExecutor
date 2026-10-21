using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.UnitTests.Fixtures;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobExecutor.UnitTests;

/// <summary>
/// The limits of the timeouts and the difference between the two phases of a run.
/// The caller must be able to tell a job that did not start from a job that did not finish.
/// </summary>
public class JobManagerTimeoutBoundaryTests
{
    /// <summary>
    /// The runtime of the framework caps the timeout at uint.MaxValue - 1 milliseconds, about
    /// 49.7 days. A longer timeout must be rejected at the registration, not in the middle of a
    /// running job.
    /// </summary>
    [Fact]
    public void AddBackgroundJobs_ShouldThrowArgumentException_WhenTheStartTimeoutIsLongerThanTheMaximum()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(options =>
            {
                options.StartTimeout = TimeSpan.FromDays(60);
                options.ExecutionTimeout = TimeSpan.FromMinutes(30);
            }));

        Assert.Contains("start timeout", exception.Message);
    }

    [Fact]
    public void AddBackgroundJobs_ShouldThrowArgumentException_WhenTheExecutionTimeoutIsLongerThanTheMaximum()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(options =>
            {
                options.StartTimeout = TimeSpan.FromMinutes(2);
                options.ExecutionTimeout = TimeSpan.FromDays(60);
            }));

        Assert.Contains("execution timeout", exception.Message);
    }

    /// <summary>
    /// A timeout below the limit of the runtime is accepted, even a long one.
    /// </summary>
    [Fact]
    public void AddBackgroundJobs_ShouldAcceptATimeoutBelowTheMaximum()
    {
        var services = new ServiceCollection();

        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(options =>
        {
            options.StartTimeout = TimeSpan.FromDays(30);
            options.ExecutionTimeout = TimeSpan.FromDays(49);
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IJobTimeouts<TestForEachJobInput, TestForEachJobResult>>().Value;

        Assert.Equal(TimeSpan.FromDays(30), options.StartTimeout);
        Assert.Equal(TimeSpan.FromDays(49), options.ExecutionTimeout);
    }

    /// <summary>
    /// A job that must live until it is stopped says so explicitly. The default of 30 minutes
    /// protects against the eternal jobs, but the choice belongs to the user.
    /// </summary>
    [Fact]
    public void AddBackgroundJobs_ShouldAcceptAnInfiniteExecutionTimeout()
    {
        var services = new ServiceCollection();

        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(options =>
        {
            options.StartTimeout = TimeSpan.FromMinutes(2);
            options.ExecutionTimeout = Timeout.InfiniteTimeSpan;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IJobTimeouts<TestForEachJobInput, TestForEachJobResult>>().Value;

        Assert.Equal(Timeout.InfiniteTimeSpan, options.ExecutionTimeout);
    }

    /// <summary>
    /// The start timeout is the wait of the caller, so it must never be infinite.
    /// </summary>
    [Fact]
    public void AddBackgroundJobs_ShouldThrowArgumentException_WhenTheStartTimeoutIsInfinite()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(options =>
            {
                options.StartTimeout = Timeout.InfiniteTimeSpan;
                options.ExecutionTimeout = TimeSpan.FromMinutes(30);
            }));

        Assert.Contains("start timeout", exception.Message);
    }

    [Fact]
    public async Task StartJobAsync_ShouldReportTheStartTimeout_WhenTheJobDoesNotStartInTime()
    {
        using var fixture = new BackgroundJobsFixture<TestSlowStartJobInput, TestSlowStartJobResult, TestSlowStartJob>(
            o => o.StartTimeout = TimeSpan.FromMilliseconds(50));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestSlowStartJobInput, TestSlowStartJobResult>>();

        var result = await manager.StartJobAsync(new JobId("slow"), new TestSlowStartJobInput());

        Assert.False(result.Success);
        Assert.Equal("Start timeout.", result.Result);
    }

    [Fact]
    public async Task RunJobAsync_ShouldReportTheStartTimeout_WhenTheJobDoesNotStartInTime()
    {
        using var fixture = new BackgroundJobsFixture<TestBlockingJobInput, TestBlockingJobResult, TestBlockingJob>(
            o =>
            {
                o.StartTimeout = TimeSpan.FromMilliseconds(100);
                o.ExecutionTimeout = TimeSpan.FromSeconds(30);
            });
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestBlockingJobInput, TestBlockingJobResult>>();
        var blockerJobId = new JobId("blocker");
        var jobId = new JobId("job-1");

        // The first job occupies the run engine, so the second request waits in the queue.
        var blockerTask = manager.RunJobAsync(blockerJobId, new TestBlockingJobInput());
        await WaitUntilJobIsRunningAsync(manager, blockerJobId);

        var result = await manager.RunJobAsync(jobId, new TestBlockingJobInput());

        Assert.False(result.Success);
        Assert.Equal("Start timeout.", result.Result);

        await manager.StopJobAsync(blockerJobId);
        await blockerTask;
    }

    [Fact]
    public async Task RunJobAsync_ShouldReportTheExecutionTimeout_WhenTheJobDoesNotFinishInTime()
    {
        using var fixture = new BackgroundJobsFixture<TestCountingJobInput, TestCountingJobResult, TestCountingJob>(
            o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(300));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCountingJobInput, TestCountingJobResult>>();

        var result = await manager.RunJobAsync(new JobId("job-1"), new TestCountingJobInput());

        Assert.False(result.Success);
        Assert.Equal("Execution timeout.", result.Result);
    }

    /// <summary>
    /// The two phases must be told apart: a job that never started and a job that did not finish
    /// in time are different problems.
    /// </summary>
    [Fact]
    public async Task RunJobAsync_ShouldReportDifferentResults_ForTheStartTimeoutAndTheExecutionTimeout()
    {
        using var fixture = new BackgroundJobsFixture<TestCountingJobInput, TestCountingJobResult, TestCountingJob>(
            o =>
            {
                o.StartTimeout = TimeSpan.FromSeconds(10);
                o.ExecutionTimeout = TimeSpan.FromMilliseconds(300);
            });
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCountingJobInput, TestCountingJobResult>>();

        var result = await manager.RunJobAsync(new JobId("job-1"), new TestCountingJobInput());

        Assert.NotEqual("Start timeout.", result.Result);
        Assert.Equal("Execution timeout.", result.Result);
    }

    private static async Task WaitUntilJobIsRunningAsync(
        IActiveJobManager<TestBlockingJobInput, TestBlockingJobResult> manager, JobId jobId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            if ((await manager.GetAllJobsAsync()).Jobs.ContainsKey(jobId))
                return;

            await Task.Delay(10);
        }

        Assert.Fail($"The job {jobId.Value} did not start in time.");
    }
}
