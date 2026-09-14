using System.Threading.Channels;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService.Processing;

internal sealed class RunJobEngine<TIn, TOut>(
    Channel<RunJobRequest<TIn>> channel,
    IRunJobCommandProcessor<TIn, TOut> commandProcessor)
    : Microsoft.Extensions.Hosting.BackgroundService
        where TIn : class
        where TOut : class
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var request in channel.Reader.ReadAllAsync(stoppingToken))
            {
                _ = commandProcessor.ProcessAsync(request, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
    }
}
