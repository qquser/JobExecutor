using System.Threading.Channels;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using Microsoft.Extensions.Logging;

namespace JobExecutor.BackgroundService.Processing;

internal sealed class RunJobEngine<TIn, TOut>(
    Channel<RunJobRequest<TIn>> channel,
    IRunJobCommandProcessor<TIn, TOut> commandProcessor,
    ILogger<RunJobEngine<TIn, TOut>> logger)
    : Microsoft.Extensions.Hosting.BackgroundService
        where TIn : class
        where TOut : class
{
    protected override async Task ExecuteAsync(CancellationToken hostStoppingToken)
    {
        try
        {
            await foreach (var request in channel.Reader.ReadAllAsync(hostStoppingToken))
            {
                _ = commandProcessor.ProcessAsync(request, hostStoppingToken);
            }
        }
        catch (OperationCanceledException) when (hostStoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("The run engine of the job type {JobType} stopped gracefully.", typeof(TIn).Name);
        }
    }
}
