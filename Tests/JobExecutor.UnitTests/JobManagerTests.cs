using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.UnitTests.Fixtures;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobExecutor.UnitTests;

public class JobManagerTests
{
    [Fact]
    public void ServiceProvider_ShouldResolveJobManager_WhenJobsRegistered()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        Assert.NotNull(manager);
    }

    [Fact]
    public void ServiceProvider_ShouldResolveJob_WhenJobsRegistered()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var job = fixture.Provider.GetService<IActiveJob<TestForEachJobInput, TestForEachJobResult>>();

        Assert.NotNull(job);
    }

    [Fact]
    public async Task RunJobAsync_ShouldReturnSuccess_WhenJobCompletes()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId(Guid.NewGuid().ToString());

        var result = await manager.RunJobAsync(jobId, new TestForEachJobInput(1));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task RunJobAsync_ShouldReturnFailure_WhenJobAlwaysThrows()
    {
        using var fixture = new BackgroundJobsFixture<TestExceptionJobInput, TestForEachJobResult, TestExceptionJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestExceptionJobInput, TestForEachJobResult>>();
        var jobId = new JobId(Guid.NewGuid().ToString());

        var result = await manager.RunJobAsync(jobId, new TestExceptionJobInput(1));

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task StartJobAsync_ShouldReturnFailure_WhenJobIdIsNullOrWhitespace(string jobId)
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        var result = await manager.StartJobAsync(new JobId(jobId), new TestForEachJobInput(1));

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunJobAsync_ShouldReturnFailure_WhenJobIdIsNullOrWhitespace(string jobId)
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        var result = await manager.RunJobAsync(new JobId(jobId), new TestForEachJobInput(1));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task StartJobAsync_ShouldReturnSuccess_WhenJobIsStarted()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId(Guid.NewGuid().ToString());

        var result = await manager.StartJobAsync(jobId, new TestForEachJobInput(100));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task StopJobAsync_ShouldReturnSuccess_WhenJobIsRunning()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId(Guid.NewGuid().ToString());
        await manager.StartJobAsync(jobId, new TestForEachJobInput(100));

        var result = await manager.StopJobAsync(jobId);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetAllJobsAsync_ShouldReturnEmpty_WhenNoJobsExist()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        var info = await manager.GetAllJobsAsync();

        Assert.Equal(0, info.TotalCount);
        Assert.Empty(info.Jobs);
    }

    [Fact]
    public async Task GetAllJobsAsync_ShouldReturnRunningJobState_WhenJobIsStarted()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId(Guid.NewGuid().ToString());
        await manager.StartJobAsync(jobId, new TestForEachJobInput(100));

        var info = await manager.GetAllJobsAsync();

        Assert.Equal(1, info.TotalCount);
        Assert.True(info.Jobs.ContainsKey(jobId));
    }

    [Fact]
    public async Task GetJobsPageAsync_ShouldReturnRunningJobState_WhenJobIsStarted()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId(Guid.NewGuid().ToString());
        await manager.StartJobAsync(jobId, new TestForEachJobInput(100));

        var info = await manager.GetJobsPageAsync(0, 10);

        Assert.Equal(1, info.TotalCount);
        Assert.True(info.Jobs.ContainsKey(jobId));
    }

    [Fact]
    public async Task GetJobsByIdsAsync_ShouldReturnRunningJobState_WhenJobIsStarted()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId(Guid.NewGuid().ToString());
        await manager.StartJobAsync(jobId, new TestForEachJobInput(100));

        var info = await manager.GetJobsByIdsAsync(new[] { jobId });

        Assert.Equal(1, info.TotalCount);
        Assert.True(info.Jobs.ContainsKey(jobId));
    }

    [Fact]
    public async Task StartJobAsync_ShouldStartSingleJob_WhenConcurrentRequestsUseSameJobId()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId(Guid.NewGuid().ToString());
        const int requestCount = 10;

        var results = await Task.WhenAll(
            Enumerable.Range(0, requestCount)
                .Select(_ => manager.StartJobAsync(jobId, new TestForEachJobInput(100))));

        Assert.Single(results, r => r.Success);
        Assert.Equal(requestCount - 1, results.Count(r => !r.Success && r.Result == $"{jobId.Value} job exists."));
    }

    [Fact]
    public async Task GetJobsPageAsync_ShouldReturnJobsOrderedByJobId_WhenPagingByOne()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        // Ids inserted out of ordinal order to prove paging sorts by JobId, not by insertion.
        var jobIds = new List<JobId> { new("c"), new("a"), new("b") };
        foreach (var jobId in jobIds)
            await manager.StartJobAsync(jobId, new TestForEachJobInput(100));

        var expected = jobIds.OrderBy(id => id.Value, StringComparer.Ordinal).ToList();
        for (var page = 0; page < expected.Count; page++)
        {
            var result = await manager.GetJobsPageAsync(page, 1);

            Assert.Equal(expected[page], result.Jobs.Keys.Single());
        }
    }

    [Fact]
    public async Task RunJobAsync_ShouldReturnCancelled_WhenJobStoppedByAnotherThread()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId(Guid.NewGuid().ToString());

        // Start a long-running job without awaiting it — it keeps running in the background.
        var runTask = manager.RunJobAsync(jobId, new TestForEachJobInput(int.MaxValue));

        // Wait until the job is actually running, then stop it from the test thread.
        await WaitUntilJobIsRunningAsync(manager, jobId);
        var stopResult = await manager.StopJobAsync(jobId);

        Assert.True(stopResult.Success);

        // The RunJobAsync caller must receive a result — not an exception, not a hang.
        var result = await runTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(result.Success);
        Assert.Equal("cancelled", result.Result);
    }

    [Fact]
    public async Task RunJobAsync_ShouldCancelRunningJob_WhenTimedOut()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(
            o => o.Timeout = TimeSpan.FromMilliseconds(50));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId(Guid.NewGuid().ToString());

        var result = await manager.RunJobAsync(jobId, new TestForEachJobInput(int.MaxValue));

        Assert.False(result.Success);
        Assert.Equal("Timeout.", result.Result);

        // The timed-out job must not keep running in the background — it is cancelled and removed.
        await WaitUntilJobRemovedAsync(manager, jobId);
        Assert.False((await manager.GetAllJobsAsync()).Jobs.ContainsKey(jobId));
    }

    [Fact]
    public async Task RunJobAsync_ShouldStartJobBeforeTimingOut_WhenExecutionExceedsTimeout()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(
            o => o.Timeout = TimeSpan.FromMilliseconds(100));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId(Guid.NewGuid().ToString());

        var runTask = manager.RunJobAsync(jobId, new TestForEachJobInput(int.MaxValue));

        // The job is started (registered) before the timeout — the timeout waits for completion, not start.
        await WaitUntilJobIsRunningAsync(manager, jobId);
        Assert.True((await manager.GetAllJobsAsync()).Jobs.ContainsKey(jobId));

        var result = await runTask;

        Assert.False(result.Success);
        Assert.Equal("Timeout.", result.Result);
    }

    [Fact]
    public async Task StartJobAsync_ShouldNotStartJob_WhenTimedOut()
    {
        using var fixture = new BackgroundJobsFixture<TestBlockingJobInput, TestBlockingJobResult, TestBlockingJob>(
            o => o.Timeout = TimeSpan.FromMilliseconds(50));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestBlockingJobInput, TestBlockingJobResult>>();
        var blockerJobId = new JobId(Guid.NewGuid().ToString());
        var timedOutJobId = new JobId(Guid.NewGuid().ToString());

        // Block the engine with a first job so the second request cannot be dequeued.
        var startResult = await manager.StartJobAsync(blockerJobId, new TestBlockingJobInput());
        Assert.True(startResult.Success);

        // The second request times out while the engine is blocked, so its cancellation is triggered.
        var result = await manager.StartJobAsync(timedOutJobId, new TestBlockingJobInput());

        Assert.False(result.Success);
        Assert.Equal("Timeout.", result.Result);

        // Unblock the engine. The cancelled request must then be skipped, not started in the background.
        await manager.StopJobAsync(blockerJobId);
        await WaitUntilJobsEmptyAsync(manager);

        Assert.Equal(0, (await manager.GetAllJobsAsync()).TotalCount);
    }

    private static async Task WaitUntilJobIsRunningAsync(
        IActiveJobManager<TestForEachJobInput, TestForEachJobResult> manager, JobId jobId)
    {
        for (var i = 0; i < 100; i++)
        {
            if ((await manager.GetAllJobsAsync()).Jobs.ContainsKey(jobId))
                return;

            await Task.Delay(10);
        }
    }

    private static async Task WaitUntilJobRemovedAsync(
        IActiveJobManager<TestForEachJobInput, TestForEachJobResult> manager, JobId jobId)
    {
        for (var i = 0; i < 100; i++)
        {
            if (!(await manager.GetAllJobsAsync()).Jobs.ContainsKey(jobId))
                return;

            await Task.Delay(10);
        }
    }

    private static async Task WaitUntilJobsEmptyAsync(
        IActiveJobManager<TestBlockingJobInput, TestBlockingJobResult> manager)
    {
        for (var i = 0; i < 100; i++)
        {
            if ((await manager.GetAllJobsAsync()).TotalCount == 0)
                return;

            await Task.Delay(10);
        }
    }
}
