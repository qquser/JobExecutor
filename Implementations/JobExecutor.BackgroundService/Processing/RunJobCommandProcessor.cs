using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobExecutor.BackgroundService.Processing;

internal sealed class RunJobCommandProcessor<TIn, TOut>(
    IServiceScopeFactory scopeFactory,
    IJobRegistry<TIn, TOut> registry,
    ILogger<RunJobCommandProcessor<TIn, TOut>> logger)
    : IRunJobCommandProcessor<TIn, TOut>
        where TIn : class
        where TOut : class
{
    public async Task ProcessAsync(RunJobRequest<TIn> request, CancellationToken stoppingToken)
    {
        if (registry.Contains(request.JobId))
        {
            request.Completed.TrySetResult(new JobCompletedResult(false, $"{request.JobId.Value} job exists."));
            return;
        }

        if (request.Cts.IsCancellationRequested)
            return;

        using var scope = scopeFactory.CreateScope();

        IActiveJob<TIn, TOut> job;
        try
        {
            job = scope.ServiceProvider.GetRequiredService<IActiveJob<TIn, TOut>>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId} could not be created.", request.JobId.Value);
            request.Completed.TrySetResult(new JobCompletedResult(false, ex.Message));
            return;
        }

        if (!registry.TryAdd(request.JobId, new RegisteredJob<TIn, TOut>(job, request.Cts)))
        {
            request.Completed.TrySetResult(new JobCompletedResult(false, $"{request.JobId.Value} job exists."));
            return;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(request.Cts.Token, stoppingToken);

        JobCompletedResult result;
        try
        {
            await job.DoAsync(request.Input, linked.Token);
            result = linked.Token.IsCancellationRequested
                ? new JobCompletedResult(false, "cancelled")
                : new JobCompletedResult(true, string.Empty);
        }
        catch (OperationCanceledException) when (linked.Token.IsCancellationRequested)
        {
            result = new JobCompletedResult(false, "cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId} failed.", request.JobId.Value);
            result = new JobCompletedResult(false, ex.Message);
        }

        request.Completed.TrySetResult(result);

        registry.TryRemove(request.JobId, out _);
    }
}
