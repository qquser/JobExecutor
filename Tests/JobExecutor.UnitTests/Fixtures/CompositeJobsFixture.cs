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
/// The order of the registrations matters: the timeout is a global option, so the timeout of the
/// last registered job wins for every job.
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

        services.AddBackgroundJobs<TestTargetJobInput, TestTargetJobResult, TestTargetJob>(configureTargetTimeout);
        services.AddBackgroundJobs<TestCallerJobInput, TestCallerJobResult, TestCallerJob>(configureTimeout);
        services.AddBackgroundJobs<TestRecursiveJobInput, TestRecursiveJobResult, TestRecursiveJob>(configureRecursiveTimeout);

        Provider = services.BuildServiceProvider();
        Tracker = Provider.GetRequiredService<JobEventTracker>();

        _hostedServices = Provider.GetServices<IHostedService>().ToArray();
        foreach (var hostedService in _hostedServices)
            hostedService.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
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
