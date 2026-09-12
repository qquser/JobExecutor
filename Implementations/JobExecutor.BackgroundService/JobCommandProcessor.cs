using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JobExecutor.BackgroundService;

internal sealed class JobCommandProcessor<TIn, TOut>(
    IServiceScopeFactory scopeFactory,
    IJobRegistry<TIn, TOut> registry,
    IJobRunner<TIn, TOut> runner)
    : IJobCommandProcessor<TIn, TOut>
    where TIn : class
    where TOut : class
{
    public async Task ProcessAsync(JobEntry<TIn, TOut> entry, CancellationToken stoppingToken)
    {
        if (!registry.TryAdd(entry))
        {
            var message = $"{entry.Run.JobId} job exists.";
            if (entry.IsCreateCommand)
                entry.Created.TrySetResult(new JobCreatedCommandResult(false, message, entry.Run.JobId));
            else
                entry.Done.TrySetResult(new JobDoneCommandResult(false, message, entry.Run.JobId));
            return;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(entry.Cts.Token, stoppingToken);
        using var scope = scopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<IJob<TIn, TOut>>();
        registry.AttachJob(entry.Run.JobId, job);

        if (entry.IsCreateCommand)
        {
            entry.Created.TrySetResult(new JobCreatedCommandResult(true, string.Empty, entry.Run.JobId));
            await runner.RunAsync(job, entry.Run, linked.Token);
        }
        else
        {
            entry.Done.TrySetResult(await runner.RunAsync(job, entry.Run, linked.Token));
        }

        registry.TryRemove(entry.Run.JobId, out _);
    }
}
