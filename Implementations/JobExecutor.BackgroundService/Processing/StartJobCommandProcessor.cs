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
    IJobTimeouts<TIn, TOut> timeouts,
    ILogger<StartJobCommandProcessor<TIn, TOut>> logger)
    : IStartJobCommandProcessor<TIn, TOut>
        where TIn : class
        where TOut : class
{
    public async Task ProcessAsync(StartJobRequest<TIn> request, CancellationToken hostStoppingToken)
    {
        if (registry.Contains(request.JobId))
        {
            request.Started.TrySetResult(new JobStartedResult(false, $"{request.JobId.Value} job exists."));
            return;
        }

        if (request.StopSource.IsCancellationRequested || request.CallerToken.IsCancellationRequested)
        {
            request.Started.TrySetResult(new JobStartedResult(false, "cancelled"));
            return;
        }

        using var scope = scopeFactory.CreateScope();

        IActiveJob<TIn, TOut> job;
        try
        {
            job = scope.ServiceProvider.GetRequiredService<IActiveJob<TIn, TOut>>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId} could not be created.", request.JobId.Value);
            request.Started.TrySetResult(new JobStartedResult(false, ex.Message));
            return;
        }

        if (!registry.TryAdd(request.JobId, new RegisteredJob<TIn, TOut>(job, request.StopSource)))
        {
            request.Started.TrySetResult(new JobStartedResult(false, $"{request.JobId.Value} job exists."));
            return;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            request.StopSource.Token,
            request.CallerToken,
            hostStoppingToken);

        using var executionTimeout = new Timer(
            _ => CancelExecution(linked, request.JobId),
            null,
            timeouts.Value.ExecutionTimeout,
            Timeout.InfiniteTimeSpan);

        if (request.StopSource.IsCancellationRequested || request.CallerToken.IsCancellationRequested)
        {
            request.Started.TrySetResult(new JobStartedResult(false, "cancelled"));
            registry.TryRemove(request.JobId, out _);
            return;
        }

        request.Started.TrySetResult(new JobStartedResult(true, string.Empty));

        try
        {
            await job.DoAsync(request.Input, linked.Token);
        }
        catch (OperationCanceledException) when (linked.Token.IsCancellationRequested)
        {
            logger.LogInformation("Job {JobId} was cancelled.", request.JobId.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId} failed.", request.JobId.Value);
        }

        registry.TryRemove(request.JobId, out _);
    }

    /// <summary>
    /// The cancellation of the job must not escape: a job that registers a throwing callback on its
    /// token makes the cancellation fail with an aggregate exception, and the timer of the library
    /// must not crash the process with it.
    /// </summary>
    private void CancelExecution(CancellationTokenSource linked, JobId jobId)
    {
        try
        {
            linked.Cancel();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "The cancellation of the job {JobId} failed.", jobId.Value);
        }
    }
}
