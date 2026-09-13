using JobExecutor.Abstractions.Interfaces;
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
        var manager = fixture.Provider.GetService<IJobManager<TestForEachJobInput, TestForEachJobResult>>();

        Assert.NotNull(manager);
    }

    [Fact]
    public void ServiceProvider_ShouldResolveJob_WhenJobsRegistered()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var job = fixture.Provider.GetService<IJob<TestForEachJobInput, TestForEachJobResult>>();

        Assert.NotNull(job);
    }

    [Fact]
    public async Task RunJobAsync_ShouldReturnSuccess_WhenJobCompletes()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();

        var result = await manager.RunJobAsync(jobId, new TestForEachJobInput(1));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task RunJobAsync_ShouldReturnFailure_WhenJobAlwaysThrows()
    {
        using var fixture = new BackgroundJobsFixture<TestExceptionJobInput, TestForEachJobResult, TestExceptionJob>();
        var manager = fixture.Provider.GetRequiredService<IJobManager<TestExceptionJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();

        var result = await manager.RunJobAsync(jobId, new TestExceptionJobInput(1),
            maxNrOfRetries: 2, minBackoff: TimeSpan.FromMilliseconds(1));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RunJobAsync_ShouldReturnSuccess_WhenFirstAttemptFailsButRetrySucceeds()
    {
        using var fixture = new BackgroundJobsFixture<TestExceptionOnFirstTryJobInput, TestExceptionOnFirstTryJobResult, TestExceptionOnFirstTryJob>();
        var manager = fixture.Provider.GetRequiredService<IJobManager<TestExceptionOnFirstTryJobInput, TestExceptionOnFirstTryJobResult>>();
        var jobId = Guid.NewGuid().ToString();

        var result = await manager.RunJobAsync(jobId, new TestExceptionOnFirstTryJobInput(1),
            maxNrOfRetries: 2, minBackoff: TimeSpan.FromMilliseconds(1));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task StartJobAsync_ShouldGenerateId_WhenJobIdIsEmpty()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IJobManager<TestForEachJobInput, TestForEachJobResult>>();

        var result = await manager.StartJobAsync(string.Empty, new TestForEachJobInput(1));

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.JobId));
    }

    [Fact]
    public async Task StartJobAsync_ShouldReturnSuccess_WhenJobIsStarted()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();

        var result = await manager.StartJobAsync(jobId, new TestForEachJobInput(100));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task StopJobAsync_ShouldReturnSuccess_WhenJobIsRunning()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();
        await manager.StartJobAsync(jobId, new TestForEachJobInput(100));

        var result = await manager.StopJobAsync(jobId);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetAllJobsAsync_ShouldReturnEmpty_WhenNoJobsExist()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IJobManager<TestForEachJobInput, TestForEachJobResult>>();

        var info = await manager.GetAllJobsAsync();

        Assert.Equal(0, info.TotalCount);
        Assert.Empty(info.Jobs);
    }

    [Fact]
    public async Task GetAllJobsAsync_ShouldReturnRunningJobState_WhenJobIsStarted()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();
        await manager.StartJobAsync(jobId, new TestForEachJobInput(100));

        var info = await manager.GetAllJobsAsync();

        Assert.Equal(1, info.TotalCount);
        Assert.True(info.Jobs[jobId].Success);
    }

    [Fact]
    public async Task GetJobsPageAsync_ShouldReturnRunningJobState_WhenJobIsStarted()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();
        await manager.StartJobAsync(jobId, new TestForEachJobInput(100));

        var info = await manager.GetJobsPageAsync(0, 10);

        Assert.Equal(1, info.TotalCount);
        Assert.True(info.Jobs[jobId].Success);
    }

    [Fact]
    public async Task GetJobsByIdsAsync_ShouldReturnRunningJobState_WhenJobIsStarted()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();
        await manager.StartJobAsync(jobId, new TestForEachJobInput(100));

        var info = await manager.GetJobsByIdsAsync(new[] { jobId });

        Assert.Equal(1, info.TotalCount);
        Assert.True(info.Jobs[jobId].Success);
    }

    [Fact]
    public async Task StartJobAsync_ShouldStartSingleJob_WhenConcurrentRequestsUseSameJobId()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();
        const int requestCount = 10;

        var results = await Task.WhenAll(
            Enumerable.Range(0, requestCount)
                .Select(_ => manager.StartJobAsync(jobId, new TestForEachJobInput(100))));

        Assert.Single(results, r => r.Success);
        Assert.Equal(requestCount - 1, results.Count(r => !r.Success && r.Result == $"{jobId} job exists."));
    }

    [Fact]
    public async Task GetJobsPageAsync_ShouldReturnJobsInStartOrder_WhenPagingByOne()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobIds = new List<string>();

        for (var i = 0; i < 3; i++)
        {
            var id = Guid.NewGuid().ToString();
            jobIds.Add(id);
            await manager.StartJobAsync(id, new TestForEachJobInput(100));
            await Task.Delay(20);
        }

        for (var page = 0; page < jobIds.Count; page++)
        {
            var result = await manager.GetJobsPageAsync(page, 1);

            Assert.Equal(jobIds[page], result.Jobs.Keys.Single());
        }
    }

    [Fact]
    public async Task RunJobAsync_ShouldReturnCancelled_WhenJobStoppedByAnotherThread()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();

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
        Assert.Equal(jobId, result.JobId);
    }

    private static async Task WaitUntilJobIsRunningAsync(
        IJobManager<TestForEachJobInput, TestForEachJobResult> manager, string jobId)
    {
        for (var i = 0; i < 100; i++)
        {
            if ((await manager.GetAllJobsAsync()).Jobs.ContainsKey(jobId))
                return;

            await Task.Delay(10);
        }
    }
}
