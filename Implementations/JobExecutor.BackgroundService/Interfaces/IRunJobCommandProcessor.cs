using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IRunJobCommandProcessor<TIn, TOut>
    where TIn : class
    where TOut : class
{
    Task ProcessAsync(RunJobRequest<TIn> request, CancellationToken stoppingToken);
}
