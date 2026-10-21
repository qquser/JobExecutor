using System.Threading.Channels;
using JobExecutor.Abstractions.Models;
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
                _ = ProcessRequestAsync(request, hostStoppingToken);
            }
        }
        catch (OperationCanceledException) when (hostStoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("The run engine of the job type {JobType} stopped gracefully.", typeof(TIn).Name);
        }
    }

    /// <summary>
    /// The request must always be answered, otherwise the caller waits for the start timeout and
    /// receives a timeout instead of the real reason.
    /// </summary>
    private async Task ProcessRequestAsync(RunJobRequest<TIn> request, CancellationToken hostStoppingToken)
    {
        try
        {
            await commandProcessor.ProcessAsync(request, hostStoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The job {JobId} could not be processed.", request.JobId.Value);
            request.Started.TrySetResult(new JobStartedResult(false, ex.Message));
            request.Completed.TrySetResult(new JobCompletedResult(false, ex.Message));
        }
    }
}
