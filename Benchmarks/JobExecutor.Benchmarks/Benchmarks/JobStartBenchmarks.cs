using BenchmarkDotNet.Attributes;
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService;
using JobExecutor.Benchmarks.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobExecutor.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class JobStartBenchmarks
{
    private readonly ServiceProvider _provider;
    private readonly IJobManager<NoOpJobInput, NoOpJobResult> _manager;
    private readonly NoOpJobInput _input;

    public JobStartBenchmarks()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundJobs<NoOpJobInput, NoOpJobResult, NoOpJob>();
        _provider = services.BuildServiceProvider();
        _manager = _provider.GetRequiredService<IJobManager<NoOpJobInput, NoOpJobResult>>();
        _input = new NoOpJobInput();

        foreach (var hostedService in _provider.GetServices<IHostedService>())
            hostedService.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var hostedService in _provider.GetServices<IHostedService>().Reverse())
            hostedService.StopAsync(CancellationToken.None).GetAwaiter().GetResult();

        _provider.Dispose();
    }

    [Benchmark(Baseline = true)]
    public Task<JobStartedResult> StartJob()
        => _manager.StartJobAsync(Guid.NewGuid().ToString(), _input);

    [Benchmark]
    public Task<JobCompletedResult> RunJob()
        => _manager.RunJobAsync(Guid.NewGuid().ToString(), _input);

    [Benchmark]
    public async Task StartJob_Concurrent1000()
    {
        var starts = Enumerable.Range(0, 1000)
            .Select(_ => _manager.StartJobAsync(Guid.NewGuid().ToString(), _input));

        await Task.WhenAll(starts);
    }
}
