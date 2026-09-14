using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobExecutor.BackgroundService.Processing;

internal sealed class JobCommandProcessor<TIn, TOut>(
    IServiceScopeFactory scopeFactory,
    IJobRegistry<TIn, TOut> registry,
    ILogger<JobCommandProcessor<TIn, TOut>> logger)
    : IJobCommandProcessor<TIn, TOut>
        where TIn : class
        where TOut : class
{
    public async Task ProcessAsync(JobRequest<TIn> request, CancellationToken stoppingToken)
    {
        if (registry.Contains(request.JobId))
        {
            var message = $"{request.JobId.Value} job exists.";
            if (request.Signals.IsStartCommand)
                request.Signals.Started.TrySetResult(new JobStartedResult(false, message));
            else
                request.Signals.Completed.TrySetResult(new JobCompletedResult(false, message));

            return;
        }

        using var scope = scopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<IActiveJob<TIn, TOut>>();

        registry.Add(request.JobId, new RegisteredJob<TIn, TOut>(job, request.Signals.Cts));

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(request.Signals.Cts.Token, stoppingToken);

        if (request.Signals.IsStartCommand)
            request.Signals.Started.TrySetResult(new JobStartedResult(true, string.Empty));

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

        if (!request.Signals.IsStartCommand)
            request.Signals.Completed.TrySetResult(result);

        registry.TryRemove(request.JobId, out _);
    }
}
