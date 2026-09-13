using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JobExecutor.BackgroundService.Processing;

internal sealed class JobCommandProcessor<TIn, TOut>(
    IServiceScopeFactory scopeFactory,
    IJobEntryFactory<TIn, TOut> entryFactory,
    IJobRegistry<TIn, TOut> registry,
    IJobRunner<TIn, TOut> runner)

    : IJobCommandProcessor<TIn, TOut>
        where TIn : class
        where TOut : class
{
    public async Task ProcessAsync(JobRequest<TIn> request, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<IJob<TIn, TOut>>();
        var entry = entryFactory.CreateEntry(request, job);

        if (!registry.TryAdd(entry))
        {
            var message = $"{entry.Run.JobId.Value} job exists.";
            if (entry.Signals.IsStartCommand)
                entry.Signals.Started.TrySetResult(new JobStartedResult(false, message, entry.Run.JobId));
            else
                entry.Signals.Completed.TrySetResult(new JobCompletedResult(false, message, entry.Run.JobId));

            return;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(entry.Signals.Cts.Token, stoppingToken);

        if (entry.Signals.IsStartCommand)
        {
            entry.Signals.Started.TrySetResult(new JobStartedResult(true, string.Empty, entry.Run.JobId));
            await runner.RunAsync(job, entry.Run, linked.Token);
        }
        else
        {
            entry.Signals.Completed.TrySetResult(await runner.RunAsync(job, entry.Run, linked.Token));
        }

        registry.TryRemove(entry.Run.JobId, out _);
    }
}
