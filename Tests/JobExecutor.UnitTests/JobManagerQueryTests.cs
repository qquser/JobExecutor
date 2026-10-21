using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.UnitTests.Fixtures;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobExecutor.UnitTests;

/// <summary>
/// The query part of the manager: the state of the running jobs.
/// Every method must answer with a result and never throw, whatever the input is.
/// </summary>
public class JobManagerQueryTests
{
    [Fact]
    public async Task GetAllJobsAsync_ShouldReturnTheStateOfEveryRunningJob()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        await manager.StartJobAsync(new JobId("job-1"), new TestForEachJobInput(100));
        await manager.StartJobAsync(new JobId("job-2"), new TestForEachJobInput(100));

        var result = await manager.GetAllJobsAsync();

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Jobs.Count);
        Assert.True(result.Jobs.ContainsKey(new JobId("job-1")));
        Assert.True(result.Jobs.ContainsKey(new JobId("job-2")));
    }

    [Fact]
    public async Task GetAllJobsAsync_ShouldReturnTheStateOfTheJob_WhileItIsWorking()
    {
        using var fixture = new BackgroundJobsFixture<TestCountingJobInput, TestCountingJobResult, TestCountingJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCountingJobInput, TestCountingJobResult>>();
        var jobId = new JobId("job-1");

        await manager.StartJobAsync(jobId, new TestCountingJobInput { Steps = 50, StepDelayMs = 50 });

        await WaitUntilAsync(() => Steps(manager, jobId) >= 2);

        Assert.Equal(1, (await manager.GetAllJobsAsync()).TotalCount);
        Assert.True(Steps(manager, jobId) >= 2);

        await manager.StopJobAsync(jobId);
    }

    [Fact]
    public async Task GetJobsPageAsync_ShouldReturnTheRequestedPage()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        foreach (var jobId in new[] { "job-c", "job-a", "job-b" })
            await manager.StartJobAsync(new JobId(jobId), new TestForEachJobInput(100));

        var firstPage = await manager.GetJobsPageAsync(0, 2);
        var secondPage = await manager.GetJobsPageAsync(2, 2);

        Assert.Equal(2, firstPage.Jobs.Count);
        Assert.Equal(new JobId("job-a"), firstPage.Jobs.Keys.First());
        Assert.Equal(new JobId("job-b"), firstPage.Jobs.Keys.Last());
        Assert.Single(secondPage.Jobs);
        Assert.Equal(new JobId("job-c"), secondPage.Jobs.Keys.Single());
    }

    [Fact]
    public async Task GetJobsPageAsync_ShouldReportTheTotalCountOfAllJobs_WhenAPageIsRequested()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        for (var index = 0; index < 3; index++)
            await manager.StartJobAsync(new JobId($"job-{index}"), new TestForEachJobInput(100));

        var page = await manager.GetJobsPageAsync(0, 2);

        Assert.Equal(2, page.Jobs.Count);
        Assert.Equal(3, page.TotalCount);
    }

    [Fact]
    public async Task GetJobsPageAsync_ShouldReturnAnEmptyPage_WhenTheSkipIsBeyondTheLastJob()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        await manager.StartJobAsync(new JobId("job-1"), new TestForEachJobInput(100));

        var page = await manager.GetJobsPageAsync(10, 10);

        Assert.Empty(page.Jobs);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task GetJobsPageAsync_ShouldReturnAnEmptyPage_WhenTheTakeIsZero()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        await manager.StartJobAsync(new JobId("job-1"), new TestForEachJobInput(100));

        var page = await manager.GetJobsPageAsync(0, 0);

        Assert.Empty(page.Jobs);
    }

    /// <summary>
    /// A negative skip is a bad request, not a fault: the manager must answer with an empty page
    /// instead of throwing.
    /// </summary>
    [Fact]
    public async Task GetJobsPageAsync_ShouldReturnAnEmptyPage_WhenTheSkipIsNegative()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        await manager.StartJobAsync(new JobId("job-1"), new TestForEachJobInput(100));

        var page = await manager.GetJobsPageAsync(-1, 10);

        Assert.Empty(page.Jobs);
    }

    /// <summary>
    /// A negative take is a bad request too.
    /// </summary>
    [Fact]
    public async Task GetJobsPageAsync_ShouldReturnAnEmptyPage_WhenTheTakeIsNegative()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        await manager.StartJobAsync(new JobId("job-1"), new TestForEachJobInput(100));

        var page = await manager.GetJobsPageAsync(0, -1);

        Assert.Empty(page.Jobs);
    }

    [Fact]
    public async Task GetJobsByIdsAsync_ShouldReturnOnlyTheRequestedJobs()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        await manager.StartJobAsync(new JobId("job-1"), new TestForEachJobInput(100));
        await manager.StartJobAsync(new JobId("job-2"), new TestForEachJobInput(100));

        var result = await manager.GetJobsByIdsAsync([new JobId("job-2")]);

        Assert.Single(result.Jobs);
        Assert.True(result.Jobs.ContainsKey(new JobId("job-2")));
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetJobsByIdsAsync_ShouldIgnoreTheUnknownIds()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        await manager.StartJobAsync(new JobId("job-1"), new TestForEachJobInput(100));

        var result = await manager.GetJobsByIdsAsync([new JobId("job-1"), new JobId("unknown")]);

        Assert.Single(result.Jobs);
    }

    /// <summary>
    /// The same identifier twice is a request of one job, not two. The manager must not fail.
    /// </summary>
    [Fact]
    public async Task GetJobsByIdsAsync_ShouldReturnTheJobOnce_WhenTheSameIdIsRequestedTwice()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        await manager.StartJobAsync(new JobId("job-1"), new TestForEachJobInput(100));

        var result = await manager.GetJobsByIdsAsync([new JobId("job-1"), new JobId("job-1")]);

        Assert.Single(result.Jobs);
        Assert.True(result.Jobs.ContainsKey(new JobId("job-1")));
    }

    [Fact]
    public async Task GetJobsByIdsAsync_ShouldReturnAnEmptyResult_WhenTheCollectionIsEmpty()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        await manager.StartJobAsync(new JobId("job-1"), new TestForEachJobInput(100));

        var result = await manager.GetJobsByIdsAsync([]);

        Assert.Empty(result.Jobs);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetJobsByIdsAsync_ShouldReturnAnEmptyResult_WhenTheCollectionIsNull()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        var result = await manager.GetJobsByIdsAsync(null!);

        Assert.Empty(result.Jobs);
        Assert.Equal(0, result.TotalCount);
    }

    /// <summary>
    /// The state is read from another thread while the job is working. The fields of the state
    /// must belong to the same step of the job, otherwise the caller sees a state that never
    /// existed.
    /// </summary>
    [Fact]
    public async Task GetAllJobsAsync_ShouldReturnAConsistentState_WhileTheJobIsWorking()
    {
        using var fixture = new BackgroundJobsFixture<TestSnapshotJobInput, TestSnapshotJobResult, TestSnapshotJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestSnapshotJobInput, TestSnapshotJobResult>>();
        var jobId = new JobId("job-1");

        await manager.StartJobAsync(jobId, new TestSnapshotJobInput { Steps = 100, GapMs = 5 });

        var inconsistent = new List<string>();

        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline)
        {
            var jobs = await manager.GetAllJobsAsync();

            if (!jobs.Jobs.TryGetValue(jobId, out var info))
                continue;

            var state = info.Result;

            if (state.DoubledSteps != state.CompletedSteps * 2 || state.Label != $"step-{state.CompletedSteps}")
                inconsistent.Add($"{state.CompletedSteps}/{state.DoubledSteps}/{state.Label}");

            if (state.CompletedSteps >= 3)
                break;
        }

        await manager.StopJobAsync(jobId);

        Assert.Empty(inconsistent);
    }

    /// <summary>
    /// Documents the limit of the live state: it is the state of the fields of the job, read from
    /// another thread. A job that publishes its fields one by one shows a mixture of the steps,
    /// because the library cannot make the getter of the job atomic. The job must publish one
    /// immutable snapshot instead.
    /// </summary>
    [Fact]
    public async Task GetAllJobsAsync_ShouldShowAMixtureOfTheSteps_WhenTheJobPublishesItsFieldsSeparately()
    {
        using var fixture = new BackgroundJobsFixture<TestTornStateJobInput, TestTornStateJobResult, TestTornStateJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestTornStateJobInput, TestTornStateJobResult>>();
        var jobId = new JobId("job-1");

        await manager.StartJobAsync(jobId, new TestTornStateJobInput { Steps = 100, GapMs = 5 });

        var inconsistent = new List<string>();

        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline)
        {
            var jobs = await manager.GetAllJobsAsync();

            if (!jobs.Jobs.TryGetValue(jobId, out var info))
                continue;

            var state = info.Result;

            if (state.DoubledSteps != state.CompletedSteps * 2 || state.Label != $"step-{state.CompletedSteps}")
                inconsistent.Add($"{state.CompletedSteps}/{state.DoubledSteps}/{state.Label}");

            if (inconsistent.Count > 0)
                break;
        }

        await manager.StopJobAsync(jobId);

        Assert.NotEmpty(inconsistent);
    }

    /// <summary>
    /// The count and the jobs of a response come from one snapshot, so they always agree. The
    /// registry changes while the query runs, and the two reads must not be independent.
    /// </summary>
    [Fact]
    public async Task GetAllJobsAsync_ShouldReportTheSameCountAsTheJobs_WhileTheJobsComeAndGo()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        var mismatches = 0;

        for (var index = 0; index < 200; index++)
        {
            _ = manager.StartJobAsync(new JobId($"job-{index}"), new TestForEachJobInput(1));

            // The registry changes while the query runs, and the two reads must not be independent.
            var result = await manager.GetAllJobsAsync();

            if (result.TotalCount != result.Jobs.Count)
                mismatches++;
        }

        Assert.Equal(0, mismatches);
    }

    private static int Steps(IActiveJobManager<TestCountingJobInput, TestCountingJobResult> manager, JobId jobId)
    {
        var jobs = manager.GetAllJobsAsync().GetAwaiter().GetResult().Jobs;

        return jobs.TryGetValue(jobId, out var info) ? info.Result.Steps : 0;
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
