using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models.Options;
using JobExecutor.BackgroundService;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace JobExecutor.UnitTests;

public class AddBackgroundJobsTests
{
    [Fact]
    public void AddBackgroundJobs_ShouldThrowInvalidOperationException_WhenSameJobTypesRegisteredTwice()
    {
        var services = new ServiceCollection();
        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(DefaultTimeout);

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(DefaultTimeout);
        });

        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void AddBackgroundJobs_ShouldThrowInvalidOperationException_WhenDifferentJobUsesSameInputOutputTypes()
    {
        var services = new ServiceCollection();
        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(DefaultTimeout);

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestSecondForEachJob>(DefaultTimeout);
        });

        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void AddBackgroundJobs_ShouldThrowInvalidOperationException_WhenSameInputTypeUsedWithDifferentOutput()
    {
        var services = new ServiceCollection();
        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(DefaultTimeout);

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddBackgroundJobs<TestForEachJobInput, TestOtherJobResult, TestSameInputDifferentOutputJob>(DefaultTimeout);
        });

        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void AddBackgroundJobs_ShouldThrowInvalidOperationException_WhenSameOutputTypeUsedWithDifferentInput()
    {
        var services = new ServiceCollection();
        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(DefaultTimeout);

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddBackgroundJobs<TestExceptionJobInput, TestForEachJobResult, TestExceptionJob>(DefaultTimeout);
        });

        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void AddBackgroundJobs_ShouldRegisterBothManagers_WhenInputAndOutputTypesDiffer()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(DefaultTimeout);
        services.AddBackgroundJobs<TestOtherJobInput, TestOtherJobResult, TestOtherJob>(DefaultTimeout);

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IActiveJobManager<TestForEachJobInput, TestForEachJobResult>>());
        Assert.NotNull(provider.GetService<IActiveJobManager<TestOtherJobInput, TestOtherJobResult>>());
    }

    /// <summary>
    /// The timeouts are mandatory at the registration, so the values of the last registration are
    /// the ones the manager uses.
    /// </summary>
    [Fact]
    public void AddBackgroundJobs_ShouldApplyTheConfiguredTimeouts()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(options =>
        {
            options.StartTimeout = TimeSpan.FromSeconds(15);
            options.ExecutionTimeout = TimeSpan.FromMinutes(7);
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JobTimeoutOptions>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(15), options.StartTimeout);
        Assert.Equal(TimeSpan.FromMinutes(7), options.ExecutionTimeout);
    }

    /// <summary>
    /// The timeouts must be positive: a broken configuration fails at the registration and not in
    /// the middle of a running job.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddBackgroundJobs_ShouldThrowArgumentException_WhenStartTimeoutIsNotPositive(int seconds)
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(options =>
            {
                options.StartTimeout = TimeSpan.FromSeconds(seconds);
                options.ExecutionTimeout = TimeSpan.FromMinutes(30);
            }));

        Assert.Contains("start timeout", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddBackgroundJobs_ShouldThrowArgumentException_WhenExecutionTimeoutIsNotPositive(int seconds)
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(options =>
            {
                options.StartTimeout = TimeSpan.FromMinutes(2);
                options.ExecutionTimeout = TimeSpan.FromSeconds(seconds);
            }));

        Assert.Contains("execution timeout", exception.Message);
    }

    /// <summary>
    /// A job must not live forever, so an infinite timeout is rejected.
    /// </summary>
    [Fact]
    public void AddBackgroundJobs_ShouldThrowArgumentException_WhenExecutionTimeoutIsInfinite()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(options =>
            {
                options.StartTimeout = TimeSpan.FromMinutes(2);
                options.ExecutionTimeout = Timeout.InfiniteTimeSpan;
            }));

        Assert.Contains("execution timeout", exception.Message);
    }

    private static void DefaultTimeout(JobTimeoutOptions options)
    {
        options.StartTimeout = TimeSpan.FromMinutes(2);
        options.ExecutionTimeout = TimeSpan.FromMinutes(30);
    }
}
