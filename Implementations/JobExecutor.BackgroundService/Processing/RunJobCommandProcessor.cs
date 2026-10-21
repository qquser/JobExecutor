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
    IJobTimeouts<TIn, TOut> timeouts,
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

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            request.StopSource.Token,
            request.CallerToken,
            hostStoppingToken);

        var executionTimedOut = 0;
        await using var executionTimeout = new Timer(
            _ => CancelExecution(linked, request.JobId, () => Interlocked.Exchange(ref executionTimedOut, 1)),
            null,
            timeouts.Value.ExecutionTimeout,
            Timeout.InfiniteTimeSpan);

        if (request.StopSource.IsCancellationRequested || request.CallerToken.IsCancellationRequested)
        {
            Fail(request, new JobCompletedResult(false, "cancelled"));
            registry.TryRemove(request.JobId, out _);
            return;
        }

        request.Started.TrySetResult(new JobStartedResult(true, string.Empty));

        JobCompletedResult result;
        try
        {
            await job.DoAsync(request.Input, linked.Token);
            result = linked.Token.IsCancellationRequested
                ? CancelledResult(Volatile.Read(ref executionTimedOut) == 1)
                : new JobCompletedResult(true, string.Empty);
        }
        catch (OperationCanceledException) when (linked.Token.IsCancellationRequested)
        {
            result = CancelledResult(Volatile.Read(ref executionTimedOut) == 1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId} failed.", request.JobId.Value);
            result = new JobCompletedResult(false, ex.Message);
        }

        registry.TryRemove(request.JobId, out _);

        request.Completed.TrySetResult(result);
    }

    /// <summary>
    /// The cancellation of the job must not escape: a job that registers a throwing callback on its
    /// token makes the cancellation fail with an aggregate exception, and the timer of the library
    /// must not crash the process with it.
    /// </summary>
    private void CancelExecution(CancellationTokenSource linked, JobId jobId, Action markTimedOut)
    {
        markTimedOut();

        try
        {
            linked.Cancel();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "The cancellation of the job {JobId} failed.", jobId.Value);
        }
    }

    /// <summary>
    /// The job was cancelled. The execution timeout is a timeout, the other reasons are a
    /// cancellation.
    /// </summary>
    private static JobCompletedResult CancelledResult(bool executionTimedOut)
        => executionTimedOut
            ? new JobCompletedResult(false, "Execution timeout.")
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
