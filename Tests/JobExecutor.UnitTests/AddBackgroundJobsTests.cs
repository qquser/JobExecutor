using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models.Options;
using JobExecutor.BackgroundService;
using JobExecutor.BackgroundService.Interfaces;
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
    /// The timeouts are mandatory at the registration, so the values of the registration are the
    /// ones the manager of that job type uses.
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
        var timeouts = provider.GetRequiredService<IJobTimeouts<TestForEachJobInput, TestForEachJobResult>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(15), timeouts.StartTimeout);
        Assert.Equal(TimeSpan.FromMinutes(7), timeouts.ExecutionTimeout);
    }

    /// <summary>
    /// Every job type keeps its own timeouts: the registration of one job type never changes the
    /// timeouts of another one.
    /// </summary>
    [Fact]
    public void AddBackgroundJobs_ShouldKeepTheTimeoutsOfEveryJobType_WhenSeveralJobsAreRegistered()
    {
        var services = new ServiceCollection();

        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(options =>
        {
            options.StartTimeout = TimeSpan.FromSeconds(15);
            options.ExecutionTimeout = TimeSpan.FromMinutes(7);
        });
        services.AddBackgroundJobs<TestOtherJobInput, TestOtherJobResult, TestOtherJob>(options =>
        {
            options.StartTimeout = TimeSpan.FromSeconds(30);
            options.ExecutionTimeout = TimeSpan.FromMinutes(9);
        });

        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IJobTimeouts<TestForEachJobInput, TestForEachJobResult>>().Value;
        var second = provider.GetRequiredService<IJobTimeouts<TestOtherJobInput, TestOtherJobResult>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(15), first.StartTimeout);
        Assert.Equal(TimeSpan.FromMinutes(7), first.ExecutionTimeout);
        Assert.Equal(TimeSpan.FromSeconds(30), second.StartTimeout);
        Assert.Equal(TimeSpan.FromMinutes(9), second.ExecutionTimeout);
    }

    /// <summary>
    /// The client may have configured the options long before the job was registered. The
    /// registration must not change the options services of the client.
    /// </summary>
    [Fact]
    public void AddBackgroundJobs_ShouldNotChangeTheOptionsServicesOfTheClient_WhenTheClientAlreadyCalledAddOptions()
    {
        var services = new ServiceCollection();
        services.AddOptions();

        var before = services.Count(descriptor => descriptor.ServiceType == typeof(IOptions<>));

        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(DefaultTimeout);

        var after = services.Count(descriptor => descriptor.ServiceType == typeof(IOptions<>));

        Assert.Equal(before, after);
    }

    /// <summary>
    /// The timeouts of a job type come from its registration. A configuration of the client does
    /// not change them, so the timeouts of a job are always the ones that were set explicitly.
    /// </summary>
    [Fact]
    public void AddBackgroundJobs_ShouldIgnoreTheConfigurationOfTheClient_WhenTheClientConfiguredTheOptions()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<JobTimeoutOptions>(options => options.ExecutionTimeout = TimeSpan.FromMinutes(1));

        services.AddBackgroundJobs<TestForEachJobInput, TestForEachJobResult, TestForEachJob>(options =>
        {
            options.StartTimeout = TimeSpan.FromMinutes(2);
            options.ExecutionTimeout = TimeSpan.FromMinutes(30);
        });

        using var provider = services.BuildServiceProvider();
        var timeouts = provider.GetRequiredService<IJobTimeouts<TestForEachJobInput, TestForEachJobResult>>().Value;

        Assert.Equal(TimeSpan.FromMinutes(30), timeouts.ExecutionTimeout);
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
    /// A job that must live until it is stopped says so explicitly: the execution timeout may be
    /// infinite. The start timeout may not, because it is the wait of the caller.
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
        var timeouts = provider.GetRequiredService<IJobTimeouts<TestForEachJobInput, TestForEachJobResult>>().Value;

        Assert.Equal(Timeout.InfiniteTimeSpan, timeouts.ExecutionTimeout);
    }

    private static void DefaultTimeout(JobTimeoutOptions options)
    {
        options.StartTimeout = TimeSpan.FromMinutes(2);
        options.ExecutionTimeout = TimeSpan.FromMinutes(30);
    }
}
