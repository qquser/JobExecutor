using System.Threading.Channels;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService;

internal sealed class JobEngine<TIn, TOut>(
    Channel<JobEntry<TIn, TOut>> channel,
    IJobCommandProcessor<TIn, TOut> commandProcessor)
    : Microsoft.Extensions.Hosting.BackgroundService
    where TIn : class
    where TOut : class
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var entry in channel.Reader.ReadAllAsync(stoppingToken))
            {
                _ = commandProcessor.ProcessAsync(entry, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
    }
}
