using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobExecutor.BackgroundService.Processing;

internal sealed class StartJobCommandProcessor<TIn, TOut>(
    IServiceScopeFactory scopeFactory,
    IJobRegistry<TIn, TOut> registry,
    ILogger<StartJobCommandProcessor<TIn, TOut>> logger)
    : IStartJobCommandProcessor<TIn, TOut>
        where TIn : class
        where TOut : class
{
    public async Task ProcessAsync(StartJobRequest<TIn> request, CancellationToken stoppingToken)
    {
        if (registry.Contains(request.JobId))
        {
            request.Started.TrySetResult(new JobStartedResult(false, $"{request.JobId.Value} job exists."));
            return;
        }

        if (request.Cts.IsCancellationRequested)
            return;

        using var scope = scopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<IActiveJob<TIn, TOut>>();

        if (!registry.TryAdd(request.JobId, new RegisteredJob<TIn, TOut>(job, request.Cts)))
        {
            request.Started.TrySetResult(new JobStartedResult(false, $"{request.JobId.Value} job exists."));
            return;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(request.Cts.Token, stoppingToken);

        request.Started.TrySetResult(new JobStartedResult(true, string.Empty));

        try
        {
            await job.DoAsync(request.Input, linked.Token);
        }
        catch (OperationCanceledException) when (linked.Token.IsCancellationRequested)
        {
            // The job was stopped; a start command has no completion signal to set.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId} failed.", request.JobId.Value);
        }

        registry.TryRemove(request.JobId, out _);
    }
}
