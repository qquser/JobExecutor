using JobExecutor.Abstractions.Interfaces;
using JobExecutor.BackgroundService;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobExecutor.UnitTests;

public class AddBackgroundJobsTests
{
    [Fact]
    public void AddBackgroundJobs_ShouldThrowInvalidOperationException_WhenSameJobTypesRegisteredTwice()
    {
        var services = new ServiceCollection();
        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        });

        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void AddBackgroundJobs_ShouldThrowInvalidOperationException_WhenDifferentJobUsesSameInputOutputTypes()
    {
        var services = new ServiceCollection();
        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestSecondForEachJob>();
        });

        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void AddBackgroundJobs_ShouldThrowInvalidOperationException_WhenSameInputTypeUsedWithDifferentOutput()
    {
        var services = new ServiceCollection();
        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddBackgroundJobs<TestForEachJobInput, TestOtherJobResult, TestSameInputDifferentOutputJob>();
        });

        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void AddBackgroundJobs_ShouldThrowInvalidOperationException_WhenSameOutputTypeUsedWithDifferentInput()
    {
        var services = new ServiceCollection();
        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddBackgroundJobs<TestExceptionJobInput, TestForEachJobResult, TestExceptionJob>();
        });

        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void AddBackgroundJobs_ShouldRegisterBothManagers_WhenInputAndOutputTypesDiffer()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>();
        services.AddBackgroundJobs<TestExceptionOnFirstTryJobInput, TestExceptionOnFirstTryJobResult, TestExceptionOnFirstTryJob>();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IJobManager<TestForEachJobInput, TestForEachJobResult>>());
        Assert.NotNull(provider.GetService<IJobManager<TestExceptionOnFirstTryJobInput, TestExceptionOnFirstTryJobResult>>());
    }
}
