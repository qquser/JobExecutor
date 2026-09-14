using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models.Options;
using JobExecutor.BackgroundService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JobExecutor.UnitTests.Fixtures;

public sealed class BackgroundJobsFixture<TIn, TOut, TJob> : IDisposable
    where TIn : class
    where TOut : class
    where TJob : class, IActiveJob<TIn, TOut>
{
    private readonly IHostedService[] _hostedServices;

    public BackgroundJobsFixture(
        Action<JobRetryOptions>? configureRetry = null,
        Action<JobTimeoutOptions>? configureTimeout = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundJobs<TIn, TOut, TJob>(configureRetry, configureTimeout);
        Provider = services.BuildServiceProvider();

        _hostedServices = Provider.GetServices<IHostedService>().ToArray();
        foreach (var hostedService in _hostedServices)
            hostedService.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public IServiceProvider Provider { get; private set; }

    public void Dispose()
    {
        foreach (var hostedService in _hostedServices.Reverse())
            hostedService.StopAsync(CancellationToken.None).GetAwaiter().GetResult();

        (Provider as IDisposable)?.Dispose();
    }
}
