using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IJobCommandProcessor<TIn, TOut>
    where TIn : class
    where TOut : class
{
    Task ProcessAsync(JobRequest<TIn> request, CancellationToken stoppingToken);
}
