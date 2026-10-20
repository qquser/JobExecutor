using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Options;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobExecutor.BackgroundService.Processing;

internal sealed class StartJobCommandProcessor<TIn, TOut>(
    IServiceScopeFactory scopeFactory,
    IJobRegistry<TIn, TOut> registry,
    IOptions<JobTimeoutOptions> timeoutOptions,
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

        // The job is cancelled by the stop source (StopJobAsync, the timeouts of the manager),
        // by the token of the caller or by the shutdown of the host. The execution timeout stops
        // the job when it lives longer than it is allowed to.
        using var executionTimeout = new CancellationTokenSource(timeoutOptions.Value.ExecutionTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            request.StopSource.Token,
            request.CallerToken,
            hostStoppingToken,
            executionTimeout.Token);

        // The start timeout of the manager may have fired while the job was being resolved: the client was
        // already rejected, so the job must not start.
        if (request.StopSource.IsCancellationRequested || request.CallerToken.IsCancellationRequested)
        {
            request.Started.TrySetResult(new JobStartedResult(false, "cancelled"));
            registry.TryRemove(request.JobId, out _);
            return;
        }

        // The job begins to execute: the caller stops waiting for the start.
        request.Started.TrySetResult(new JobStartedResult(true, string.Empty));

        try
        {
            await job.DoAsync(request.Input, linked.Token);
        }
        catch (OperationCanceledException) when (linked.Token.IsCancellationRequested)
        {
            // The job was stopped; a start command has no completion signal to set.
            logger.LogInformation("Job {JobId} was cancelled.", request.JobId.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId} failed.", request.JobId.Value);
        }

        registry.TryRemove(request.JobId, out _);
    }
}
