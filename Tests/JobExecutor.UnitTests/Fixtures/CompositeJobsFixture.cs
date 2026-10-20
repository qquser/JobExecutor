using JobExecutor.Abstractions.Models.Options;
using JobExecutor.BackgroundService;
using JobExecutor.UnitTests.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JobExecutor.UnitTests.Fixtures;

/// <summary>
/// Registers several job types at once, so a job can call another job.
/// </summary>
/// <remarks>
/// The order of the registrations matters: the timeouts are global options, so the timeouts of the
/// last registered job win for every job.
/// </remarks>
public sealed class CompositeJobsFixture : IDisposable
{
    private readonly IHostedService[] _hostedServices;

    public CompositeJobsFixture(
        Action<JobTimeoutOptions>? configureTimeout = null,
        Action<JobTimeoutOptions>? configureTargetTimeout = null,
        Action<JobTimeoutOptions>? configureRecursiveTimeout = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<JobEventTracker>();

        // The timeouts are global options, so the last registration wins. A registration without its
        // own configuration keeps the configuration of the previous one.
        var effectiveTimeout = configureTimeout ?? DefaultTimeout;
        var effectiveTargetTimeout = configureTargetTimeout ?? effectiveTimeout;
        var effectiveRecursiveTimeout = configureRecursiveTimeout ?? effectiveTimeout;

        services.AddBackgroundJobs<TestTargetJobInput, TestTargetJobResult, TestTargetJob>(effectiveTargetTimeout);
        services.AddBackgroundJobs<TestCallerJobInput, TestCallerJobResult, TestCallerJob>(effectiveTimeout);
        services.AddBackgroundJobs<TestRecursiveJobInput, TestRecursiveJobResult, TestRecursiveJob>(effectiveRecursiveTimeout);

        Provider = services.BuildServiceProvider();
        Tracker = Provider.GetRequiredService<JobEventTracker>();

        _hostedServices = Provider.GetServices<IHostedService>().ToArray();
        foreach (var hostedService in _hostedServices)
            hostedService.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// The default timeouts of the library: 2 minutes for the start and 30 minutes for the whole
    /// life of the job.
    /// </summary>
    private static void DefaultTimeout(JobTimeoutOptions options)
    {
        options.StartTimeout = TimeSpan.FromMinutes(2);
        options.ExecutionTimeout = TimeSpan.FromMinutes(30);
    }

    public IServiceProvider Provider { get; private set; }

    public JobEventTracker Tracker { get; private set; }

    public void Dispose()
    {
        foreach (var hostedService in _hostedServices.Reverse())
            hostedService.StopAsync(CancellationToken.None).GetAwaiter().GetResult();

        (Provider as IDisposable)?.Dispose();
    }
}
