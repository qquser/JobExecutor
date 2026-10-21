using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.UnitTests.Fixtures;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobExecutor.UnitTests;

/// <summary>
/// The command part of the manager: start, run and stop.
/// Every method must answer with a result and never throw, whatever happens.
/// </summary>
public class JobManagerContractTests
{
    [Fact]
    public async Task StartJobAsync_ShouldReturnTheJobAsRunning_WhenItReturns()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId("job-1");

        var result = await manager.StartJobAsync(jobId, new TestForEachJobInput(100));

        Assert.True(result.Success);
        Assert.True((await manager.GetAllJobsAsync()).Jobs.ContainsKey(jobId));
    }

    [Fact]
    public async Task StartJobAsync_ShouldRejectTheSecondSubmissionOfTheSameJobId()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId("job-1");

        var first = await manager.StartJobAsync(jobId, new TestForEachJobInput(100));
        var second = await manager.StartJobAsync(jobId, new TestForEachJobInput(100));

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Equal("job-1 job exists.", second.Result);
    }

    /// <summary>
    /// The start and the run share one registry of the job type, so a running job cannot be run
    /// a second time under the same identifier.
    /// </summary>
    [Fact]
    public async Task RunJobAsync_ShouldRejectTheJob_WhenAJobWithTheSameIdIsAlreadyRunning()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId("job-1");

        await manager.StartJobAsync(jobId, new TestForEachJobInput(100));

        var result = await manager.RunJobAsync(jobId, new TestForEachJobInput(1));

        Assert.False(result.Success);
        Assert.Equal("job-1 job exists.", result.Result);
    }

    /// <summary>
    /// Every job type has its own registry, so the same identifier belongs to different jobs of
    /// different types.
    /// </summary>
    [Fact]
    public async Task StartJobAsync_ShouldAcceptTheSameJobId_ForAnotherJobType()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        using var otherFixture = new BackgroundJobsFixture<TestOtherJobInput, TestOtherJobResult, TestOtherJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var otherManager = otherFixture.Provider.GetRequiredService<IActiveJobManager<TestOtherJobInput, TestOtherJobResult>>();
        var jobId = new JobId("job-1");

        var first = await manager.StartJobAsync(jobId, new TestForEachJobInput(100));
        var second = await otherManager.StartJobAsync(jobId, new TestOtherJobInput());

        Assert.True(first.Success);
        Assert.True(second.Success);
    }

    [Fact]
    public async Task StopJobAsync_ShouldReturnFailure_WhenTheJobDoesNotExist()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        var result = await manager.StopJobAsync(new JobId("unknown"));

        Assert.False(result.Success);
        Assert.Contains("unknown", result.Result);
    }

    /// <summary>
    /// Documents the hole: a job that waits in the queue is not in the registry yet, so it cannot
    /// be stopped by its identifier. It starts anyway when the engine becomes free.
    /// </summary>
    [Fact]
    public async Task StopJobAsync_ShouldNotStopAJobThatIsStillInTheQueue()
    {
        using var fixture = new BackgroundJobsFixture<TestBlockingJobInput, TestBlockingJobResult, TestBlockingJob>(
            o => o.StartTimeout = TimeSpan.FromSeconds(10));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestBlockingJobInput, TestBlockingJobResult>>();
        var blockerJobId = new JobId("blocker");
        var queuedJobId = new JobId("queued");

        // The first job occupies the engine, so the second request waits in the queue.
        await manager.StartJobAsync(blockerJobId, new TestBlockingJobInput());
        await WaitUntilJobIsRunningAsync(manager, blockerJobId);

        var queuedTask = manager.StartJobAsync(queuedJobId, new TestBlockingJobInput());

        // The queued job is not in the registry yet.
        var stopResult = await manager.StopJobAsync(queuedJobId);

        Assert.False(stopResult.Success);

        // The engine becomes free and the queued job starts anyway.
        await manager.StopJobAsync(blockerJobId);
        await WaitUntilJobIsRunningAsync(manager, queuedJobId);

        Assert.True((await manager.GetAllJobsAsync()).Jobs.ContainsKey(queuedJobId));

        await manager.StopJobAsync(queuedJobId);
        await queuedTask;
    }

    /// <summary>
    /// A job that fails with an exception is not a cancellation: the caller must be able to tell
    /// the difference.
    /// </summary>
    [Fact]
    public async Task RunJobAsync_ShouldReportTheFailureOfTheJob_AndNotTheCancellation_WhenTheJobThrowsOperationCanceled()
    {
        using var fixture = new BackgroundJobsFixture<TestCancellableJobInput, TestCancellableJobResult, TestCancellableJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult>>();

        var result = await manager.RunJobAsync(
            new JobId("job-1"),
            new TestCancellableJobInput { ThrowOperationCanceledException = true });

        Assert.False(result.Success);
        Assert.NotEqual("cancelled", result.Result);
    }

    [Fact]
    public async Task RunJobAsync_ShouldReturnTheMessageOfTheException_WhenTheJobFails()
    {
        using var fixture = new BackgroundJobsFixture<TestExceptionJobInput, TestForEachJobResult, TestExceptionJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestExceptionJobInput, TestForEachJobResult>>();

        var result = await manager.RunJobAsync(new JobId("job-1"), new TestExceptionJobInput(1));

        Assert.False(result.Success);
        Assert.Equal("Test exception.", result.Result);
    }

    [Fact]
    public async Task RunJobAsync_ShouldRejectTheSecondSubmissionOfTheSameJobId()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId("job-1");

        var first = manager.RunJobAsync(jobId, new TestForEachJobInput(100));
        var second = manager.RunJobAsync(jobId, new TestForEachJobInput(100));

        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => result.Success);
        Assert.Single(results, result => !result.Success && result.Result == "job-1 job exists.");
    }

    /// <summary>
    /// The run of a job that is stopped by another thread must answer with the cancellation, not
    /// with an exception and not with a hang.
    /// </summary>
    [Fact]
    public async Task RunJobAsync_ShouldReportTheCancellation_WhenTheJobIsStoppedWhileItRuns()
    {
        using var fixture = new BackgroundJobsFixture<TestCancellableJobInput, TestCancellableJobResult, TestCancellableJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult>>();
        var jobId = new JobId("job-1");

        var runTask = manager.RunJobAsync(jobId, new TestCancellableJobInput { BlockUntilCancelled = true });
        await WaitUntilJobIsRunningAsync(manager, jobId);

        await manager.StopJobAsync(jobId);

        var result = await runTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(result.Success);
        Assert.Equal("cancelled", result.Result);
    }

    /// <summary>
    /// The caller of a run that is stopped by another thread must not hang: the call returns.
    /// </summary>
    [Fact]
    public async Task RunJobAsync_ShouldReturn_WhenTheJobIsStoppedWhileItRuns()
    {
        using var fixture = new BackgroundJobsFixture<TestCancellableJobInput, TestCancellableJobResult, TestCancellableJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult>>();
        var jobId = new JobId("job-1");

        var runTask = manager.RunJobAsync(jobId, new TestCancellableJobInput { BlockUntilCancelled = true });
        await WaitUntilJobIsRunningAsync(manager, jobId);

        await manager.StopJobAsync(jobId);

        var result = await runTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotNull(result);
    }

    /// <summary>
    /// The identifier of the job is required, and the answer is a result, not an exception.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task StartJobAsync_ShouldReturnFailure_WhenTheJobIdIsMissing(string? jobId)
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        var result = await manager.StartJobAsync(new JobId(jobId!), new TestForEachJobInput(1));

        Assert.False(result.Success);
        Assert.Equal("Job id is required.", result.Result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunJobAsync_ShouldReturnFailure_WhenTheJobIdIsMissing(string? jobId)
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        var result = await manager.RunJobAsync(new JobId(jobId!), new TestForEachJobInput(1));

        Assert.False(result.Success);
        Assert.Equal("Job id is required.", result.Result);
    }

    /// <summary>
    /// A missing identifier object is invalid input, not a fault: the manager answers with a
    /// result.
    /// </summary>
    [Fact]
    public async Task StartJobAsync_ShouldReturnFailure_WhenTheJobIdIsNull()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        var result = await manager.StartJobAsync(null!, new TestForEachJobInput(1));

        Assert.False(result.Success);
        Assert.Equal("Job id is required.", result.Result);
    }

    [Fact]
    public async Task RunJobAsync_ShouldReturnFailure_WhenTheJobIdIsNull()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        var result = await manager.RunJobAsync(null!, new TestForEachJobInput(1));

        Assert.False(result.Success);
        Assert.Equal("Job id is required.", result.Result);
    }

    [Fact]
    public async Task StopJobAsync_ShouldReturnFailure_WhenTheJobIdIsNull()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        var result = await manager.StopJobAsync(null!);

        Assert.False(result.Success);
        Assert.Equal("Job id is required.", result.Result);
    }

    /// <summary>
    /// A job that throws in its cancellation callback makes the cancellation fail. The manager
    /// must still answer with the result of the command.
    /// </summary>
    [Fact]
    public async Task StopJobAsync_ShouldReturnTheResult_WhenTheJobThrowsInItsCancellationCallback()
    {
        using var fixture = new BackgroundJobsFixture<TestThrowingCallbackJobInput, TestThrowingCallbackJobResult, TestThrowingCallbackJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestThrowingCallbackJobInput, TestThrowingCallbackJobResult>>();
        var jobId = new JobId("job-1");

        var startResult = await manager.StartJobAsync(jobId, new TestThrowingCallbackJobInput());
        Assert.True(startResult.Success);

        var result = await manager.StopJobAsync(jobId);

        Assert.True(result.Success);
    }

    /// <summary>
    /// The same for the timeout of the run: the job throws in its cancellation callback, and the
    /// caller still receives the timeout instead of an exception.
    /// </summary>
    [Fact]
    public async Task RunJobAsync_ShouldReturnTheTimeout_WhenTheJobThrowsInItsCancellationCallback()
    {
        using var fixture = new BackgroundJobsFixture<TestThrowingCallbackJobInput, TestThrowingCallbackJobResult, TestThrowingCallbackJob>(
            o => o.ExecutionTimeout = TimeSpan.FromMilliseconds(300));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestThrowingCallbackJobInput, TestThrowingCallbackJobResult>>();

        var result = await manager.RunJobAsync(new JobId("job-1"), new TestThrowingCallbackJobInput());

        Assert.False(result.Success);
        Assert.Equal("Execution timeout.", result.Result);
    }

    /// <summary>
    /// A collection with a missing identifier is invalid input, not a fault.
    /// </summary>
    [Fact]
    public async Task GetJobsByIdsAsync_ShouldReturnTheJobs_WhenTheCollectionContainsAMissingId()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        await manager.StartJobAsync(new JobId("job-1"), new TestForEachJobInput(100));

        var result = await manager.GetJobsByIdsAsync([new JobId("job-1"), null!]);

        Assert.Single(result.Jobs);
        Assert.True(result.Jobs.ContainsKey(new JobId("job-1")));
    }

    private static async Task WaitUntilJobIsRunningAsync(
        IActiveJobManager<TestBlockingJobInput, TestBlockingJobResult> manager, JobId jobId)
    {
        await WaitUntilAsync(() => manager.GetAllJobsAsync().GetAwaiter().GetResult().Jobs.ContainsKey(jobId));
    }

    private static async Task WaitUntilJobIsRunningAsync(
        IActiveJobManager<TestCancellableJobInput, TestCancellableJobResult> manager, JobId jobId)
    {
        await WaitUntilAsync(() => manager.GetAllJobsAsync().GetAwaiter().GetResult().Jobs.ContainsKey(jobId));
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000)
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
