using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.UnitTests.Fixtures;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobExecutor.UnitTests;

public class ProductionEdgeCaseTests
{
    [Fact]
    public async Task StartJobAsync_ShouldReturnFailure_WhenJobCannotBeResolved()
    {
        using var fixture = new BackgroundJobsFixture<TestUnresolvableJobInput, TestUnresolvableJobResult, TestUnresolvableJob>(
            o => o.Timeout = TimeSpan.FromMilliseconds(200));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestUnresolvableJobInput, TestUnresolvableJobResult>>();
        var jobId = new JobId(Guid.NewGuid().ToString());

        var result = await manager.StartJobAsync(jobId, new TestUnresolvableJobInput());

        Assert.False(result.Success);
        Assert.NotEqual("Timeout.", result.Result);
    }

    [Fact]
    public async Task RunJobAsync_ShouldReturnFailure_WhenJobCannotBeResolved()
    {
        using var fixture = new BackgroundJobsFixture<TestUnresolvableJobInput, TestUnresolvableJobResult, TestUnresolvableJob>(
            o => o.Timeout = TimeSpan.FromMilliseconds(200));
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestUnresolvableJobInput, TestUnresolvableJobResult>>();
        var jobId = new JobId(Guid.NewGuid().ToString());

        var result = await manager.RunJobAsync(jobId, new TestUnresolvableJobInput());

        Assert.False(result.Success);
        Assert.NotEqual("Timeout.", result.Result);
    }

    [Fact]
    public async Task GetJobsByIdsAsync_ShouldReturnEmpty_WhenJobIdsIsNull()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();

        var info = await manager.GetJobsByIdsAsync(null!);

        Assert.Equal(0, info.TotalCount);
        Assert.Empty(info.Jobs);
    }

    [Fact]
    public async Task RunJobAsync_ShouldReturnExists_WhenJobIdAlreadyStarted()
    {
        using var fixture = new BackgroundJobsFixture<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        var manager = fixture.Provider.GetRequiredService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>();
        var jobId = new JobId(Guid.NewGuid().ToString());

        await manager.StartJobAsync(jobId, new TestForEachJobInput(int.MaxValue));

        var result = await manager.RunJobAsync(jobId, new TestForEachJobInput(1));

        Assert.False(result.Success);
        Assert.Equal($"{jobId.Value} job exists.", result.Result);
    }
}
