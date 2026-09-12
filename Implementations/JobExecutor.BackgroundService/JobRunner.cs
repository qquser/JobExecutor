using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService;

internal sealed class JobRunner<TIn, TOut> : IJobRunner<TIn, TOut>
    where TIn : class
    where TOut : class
{
    public async Task<JobDoneCommandResult> RunAsync(IJob<TIn, TOut> job, JobRunModel<TIn> run, 
        CancellationToken token)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var success = await job.DoAsync(run.Input, token);
                return success
                    ? new JobDoneCommandResult(true, string.Empty, run.JobId)
                    : new JobDoneCommandResult(false, "cancelled", run.JobId);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return new JobDoneCommandResult(false, "cancelled", run.JobId);
            }
            catch (Exception ex)
            {
                if (attempt >= run.MaxNrOfRetries)
                    return new JobDoneCommandResult(false, ex.Message, run.JobId);

                await Task.Delay(run.Backoff, token);
            }
        }
    }
}
