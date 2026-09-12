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
public class JobCreationBenchmarks
{
    private readonly ServiceProvider _provider;
    private readonly IJobContext<NoOpJobInput, NoOpJobResult> _context;
    private readonly NoOpJobInput _input;

    public JobCreationBenchmarks()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundJobs<NoOpJobInput, NoOpJobResult, NoOpJob>();
        _provider = services.BuildServiceProvider();
        _context = _provider.GetRequiredService<IJobContext<NoOpJobInput, NoOpJobResult>>();
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
    public Task<JobCreatedCommandResult> CreateJob()
        => _context.CreateJobAsync(Guid.NewGuid().ToString(), _input);

    [Benchmark]
    public Task<JobDoneCommandResult> DoJob()
        => _context.DoJobAsync(Guid.NewGuid().ToString(), _input);

    [Benchmark]
    public async Task CreateJob_Concurrent1000()
    {
        var creates = Enumerable.Range(0, 1000)
            .Select(_ => _context.CreateJobAsync(Guid.NewGuid().ToString(), _input));

        await Task.WhenAll(creates);
    }
}
