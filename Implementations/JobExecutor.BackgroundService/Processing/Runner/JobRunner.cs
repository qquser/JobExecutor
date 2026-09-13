using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using Microsoft.Extensions.Logging;

namespace JobExecutor.BackgroundService.Processing.Runner;

internal sealed class JobRunner<TIn, TOut>(ILogger<JobRunner<TIn, TOut>> logger)
    : IJobRunner<TIn, TOut>
    where TIn : class
    where TOut : class
{
    public async Task<JobCompletedResult> RunAsync(IJob<TIn, TOut> job, JobRunModel<TIn> run,
        CancellationToken token)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var success = await job.DoAsync(run.Input, token);
                return success
                    ? new JobCompletedResult(true, string.Empty, run.JobId)
                    : new JobCompletedResult(false, "cancelled", run.JobId);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return new JobCompletedResult(false, "cancelled", run.JobId);
            }
            catch (Exception ex)
            {
                if (attempt >= run.MaxNrOfRetries)
                {
                    logger.LogError(ex, "Job {JobId} failed after {Attempts} attempts, retries exhausted.", run.JobId.Value, attempt + 1);
                    return new JobCompletedResult(false, ex.Message, run.JobId);
                }

                logger.LogWarning(ex, "Job {JobId} attempt {Attempt} failed, retrying.", run.JobId.Value, attempt + 1);
                await Task.Delay(run.Backoff, token);
            }
        }
    }
}
