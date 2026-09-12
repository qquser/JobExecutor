using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JobExecutor.BackgroundService;

internal sealed class JobCommandProcessor<TIn, TOut> : IJobCommandProcessor<TIn, TOut>
    where TIn : class
    where TOut : class
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IJobRegistry<TIn, TOut> _registry;
    private readonly IJobRunner<TIn, TOut> _runner;

    public JobCommandProcessor(IServiceScopeFactory scopeFactory, IJobRegistry<TIn, TOut> registry, IJobRunner<TIn, TOut> runner)
    {
        _scopeFactory = scopeFactory;
        _registry = registry;
        _runner = runner;
    }

    public async Task ProcessAsync(JobEntry<TIn, TOut> entry, CancellationToken stoppingToken)
    {
        if (!_registry.TryAdd(entry))
        {
            var message = $"{entry.Run.JobId} job exists.";
            if (entry.IsCreateCommand)
                entry.Created.TrySetResult(new JobCreatedCommandResult(false, message, entry.Run.JobId));
            else
                entry.Done.TrySetResult(new JobDoneCommandResult(false, message, entry.Run.JobId));
            return;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(entry.Cts.Token, stoppingToken);
        using var scope = _scopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<IJob<TIn, TOut>>();
        _registry.AttachJob(entry.Run.JobId, job);

        if (entry.IsCreateCommand)
        {
            entry.Created.TrySetResult(new JobCreatedCommandResult(true, string.Empty, entry.Run.JobId));
            await _runner.RunAsync(job, entry.Run, linked.Token);
        }
        else
        {
            entry.Done.TrySetResult(await _runner.RunAsync(job, entry.Run, linked.Token));
        }

        _registry.TryRemove(entry.Run.JobId, out _);
    }
}
