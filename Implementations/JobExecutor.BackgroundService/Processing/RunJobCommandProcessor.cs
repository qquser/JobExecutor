using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Options;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobExecutor.BackgroundService.Processing;

internal sealed class RunJobCommandProcessor<TIn, TOut>(
    IServiceScopeFactory scopeFactory,
    IJobRegistry<TIn, TOut> registry,
    IOptions<JobTimeoutOptions> timeoutOptions,
    ILogger<RunJobCommandProcessor<TIn, TOut>> logger)
    : IRunJobCommandProcessor<TIn, TOut>
        where TIn : class
        where TOut : class
{
    public async Task ProcessAsync(RunJobRequest<TIn> request, CancellationToken hostStoppingToken)
    {
        if (registry.Contains(request.JobId))
        {
            Fail(request, new JobCompletedResult(false, $"{request.JobId.Value} job exists."));
            return;
        }

        if (request.StopSource.IsCancellationRequested || request.CallerToken.IsCancellationRequested)
        {
            Fail(request, new JobCompletedResult(false, "cancelled"));
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
            Fail(request, new JobCompletedResult(false, ex.Message));
            return;
        }

        if (!registry.TryAdd(request.JobId, new RegisteredJob<TIn, TOut>(job, request.StopSource)))
        {
            Fail(request, new JobCompletedResult(false, $"{request.JobId.Value} job exists."));
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
            Fail(request, new JobCompletedResult(false, "cancelled"));
            registry.TryRemove(request.JobId, out _);
            return;
        }

        // The job begins to execute: the caller stops waiting for the start and waits for the
        // completion instead.
        request.Started.TrySetResult(new JobStartedResult(true, string.Empty));

        JobCompletedResult result;
        try
        {
            await job.DoAsync(request.Input, linked.Token);
            result = linked.Token.IsCancellationRequested
                ? CancelledResult(executionTimeout)
                : new JobCompletedResult(true, string.Empty);
        }
        catch (OperationCanceledException) when (linked.Token.IsCancellationRequested)
        {
            result = CancelledResult(executionTimeout);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId} failed.", request.JobId.Value);
            result = new JobCompletedResult(false, ex.Message);
        }

        request.Completed.TrySetResult(result);

        registry.TryRemove(request.JobId, out _);
    }

    /// <summary>
    /// The job was cancelled. The execution timeout is a timeout, the other reasons are a
    /// cancellation.
    /// </summary>
    private static JobCompletedResult CancelledResult(CancellationTokenSource executionTimeout)
        => executionTimeout.IsCancellationRequested
            ? new JobCompletedResult(false, "Timeout.")
            : new JobCompletedResult(false, "cancelled");

    /// <summary>
    /// The request cannot be executed: the caller must learn about it at once, so both signals are
    /// set.
    /// </summary>
    private static void Fail(RunJobRequest<TIn> request, JobCompletedResult result)
    {
        request.Started.TrySetResult(new JobStartedResult(false, result.Result));
        request.Completed.TrySetResult(result);
    }
}
