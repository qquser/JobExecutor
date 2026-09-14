using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Interfaces;

internal interface IStartJobCommandProcessor<TIn, TOut>
    where TIn : class
    where TOut : class
{
    Task ProcessAsync(StartJobRequest<TIn> request, CancellationToken stoppingToken);
}
