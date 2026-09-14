using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IJobRunner<TIn, TOut>
    where TIn : class
    where TOut : class
{
    Task<JobCompletedResult> RunAsync(IActiveJob<TIn, TOut> job, JobRunModel<TIn> run, CancellationToken token);
}
