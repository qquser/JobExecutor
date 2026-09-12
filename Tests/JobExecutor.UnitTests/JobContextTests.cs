using JobExecutor.Abstractions.Interfaces;
using JobExecutor.UnitTests.Fixtures;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobExecutor.UnitTests;

public class JobContextTests
{
    [Fact]
    public void ServiceProvider_ShouldResolveJobContext_WhenJobsRegistered()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var context = fixture.Provider.GetService<IJobContext<TestForEachJobInput, TestForEachJobResult>>();

        Assert.NotNull(context);
    }

    [Fact]
    public void ServiceProvider_ShouldResolveJob_WhenJobsRegistered()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var job = fixture.Provider.GetService<IJob<TestForEachJobInput, TestForEachJobResult>>();

        Assert.NotNull(job);
    }

    [Fact]
    public async Task DoJobAsync_ShouldReturnSuccess_WhenJobCompletes()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var context = fixture.Provider.GetRequiredService<IJobContext<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();

        var result = await context.DoJobAsync(jobId, new TestForEachJobInput(1));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task DoJobAsync_ShouldReturnFailure_WhenJobAlwaysThrows()
    {
        using var fixture = new BackgroundJobsFixture<TestExceptionJobInput, TestForEachJobResult, TestExceptionJob>();
        var context = fixture.Provider.GetRequiredService<IJobContext<TestExceptionJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();

        var result = await context.DoJobAsync(jobId, new TestExceptionJobInput(1),
            maxNrOfRetries: 2, minBackoff: TimeSpan.FromMilliseconds(1));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DoJobAsync_ShouldReturnSuccess_WhenFirstAttemptFailsButRetrySucceeds()
    {
        using var fixture = new BackgroundJobsFixture<TestExceptionOnFirstTryJobInput, TestForEachJobResult, TestExceptionOnFirstTryJob>();
        var context = fixture.Provider.GetRequiredService<IJobContext<TestExceptionOnFirstTryJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();

        var result = await context.DoJobAsync(jobId, new TestExceptionOnFirstTryJobInput(1),
            maxNrOfRetries: 2, minBackoff: TimeSpan.FromMilliseconds(1));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateJobAsync_ShouldGenerateId_WhenJobIdIsEmpty()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var context = fixture.Provider.GetRequiredService<IJobContext<TestForEachJobInput, TestForEachJobResult>>();

        var result = await context.CreateJobAsync(string.Empty, new TestForEachJobInput(1));

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.JobId));
    }

    [Fact]
    public async Task CreateJobAsync_ShouldReturnSuccess_WhenJobIsStarted()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var context = fixture.Provider.GetRequiredService<IJobContext<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();

        var result = await context.CreateJobAsync(jobId, new TestForEachJobInput(100));

        Assert.True(result.Success);
    }

    [Fact]
    public async Task StopJobAsync_ShouldReturnSuccess_WhenJobIsRunning()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var context = fixture.Provider.GetRequiredService<IJobContext<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();
        await context.CreateJobAsync(jobId, new TestForEachJobInput(100));

        var result = await context.StopJobAsync(jobId);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetAllJobsAsync_ShouldReturnEmpty_WhenNoJobsExist()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var context = fixture.Provider.GetRequiredService<IJobContext<TestForEachJobInput, TestForEachJobResult>>();

        var info = await context.GetAllJobsAsync();

        Assert.Equal(0, info.TotalCount);
        Assert.Empty(info.WorkersData);
    }

    [Fact]
    public async Task GetAllJobsAsync_ShouldReturnRunningJobState_WhenJobIsStarted()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var context = fixture.Provider.GetRequiredService<IJobContext<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();
        await context.CreateJobAsync(jobId, new TestForEachJobInput(100));

        var info = await context.GetAllJobsAsync();

        Assert.Equal(1, info.TotalCount);
        Assert.True(info.WorkersData[jobId].Success);
    }

    [Fact]
    public async Task GetJobsPaginateAsync_ShouldReturnRunningJobState_WhenJobIsStarted()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var context = fixture.Provider.GetRequiredService<IJobContext<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();
        await context.CreateJobAsync(jobId, new TestForEachJobInput(100));

        var info = await context.GetJobsPaginateAsync(0, 10);

        Assert.Equal(1, info.TotalCount);
        Assert.True(info.WorkersData[jobId].Success);
    }

    [Fact]
    public async Task GetJobsByIdsAsync_ShouldReturnRunningJobState_WhenJobIsStarted()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var context = fixture.Provider.GetRequiredService<IJobContext<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();
        await context.CreateJobAsync(jobId, new TestForEachJobInput(100));

        var info = await context.GetJobsByIdsAsync(new[] { jobId });

        Assert.Equal(1, info.TotalCount);
        Assert.True(info.WorkersData[jobId].Success);
    }

    [Fact]
    public async Task CreateJobAsync_ShouldCreateSingleJob_WhenConcurrentRequestsUseSameJobId()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var context = fixture.Provider.GetRequiredService<IJobContext<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = Guid.NewGuid().ToString();
        const int requestCount = 10;

        var results = await Task.WhenAll(
            Enumerable.Range(0, requestCount)
                .Select(_ => context.CreateJobAsync(jobId, new TestForEachJobInput(100))));

        Assert.Single(results, r => r.Success);
        Assert.Equal(requestCount - 1, results.Count(r => !r.Success && r.Result == $"{jobId} job exists."));
    }

    [Fact]
    public async Task GetJobsPaginateAsync_ShouldReturnJobsInCreationOrder_WhenPagingByOne()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var context = fixture.Provider.GetRequiredService<IJobContext<TestForEachJobInput, TestForEachJobResult>>();
        var jobIds = new List<string>();

        for (var i = 0; i < 3; i++)
        {
            var id = Guid.NewGuid().ToString();
            jobIds.Add(id);
            await context.CreateJobAsync(id, new TestForEachJobInput(100));
            await Task.Delay(20);
        }

        for (var page = 0; page < jobIds.Count; page++)
        {
            var result = await context.GetJobsPaginateAsync(page, 1);

            Assert.Equal(jobIds[page], result.WorkersData.Keys.Single());
        }
    }
}
