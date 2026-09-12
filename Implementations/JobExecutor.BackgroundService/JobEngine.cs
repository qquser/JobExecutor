using System.Threading.Channels;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;

namespace JobExecutor.BackgroundService;

internal sealed class JobEngine<TIn, TOut> : Microsoft.Extensions.Hosting.BackgroundService
    where TIn : class
    where TOut : class
{
    private readonly Channel<JobEntry<TIn, TOut>> _channel;
    private readonly IJobCommandProcessor<TIn, TOut> _commandProcessor;

    public JobEngine(Channel<JobEntry<TIn, TOut>> channel, IJobCommandProcessor<TIn, TOut> commandProcessor)
    {
        _channel = channel;
        _commandProcessor = commandProcessor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var entry in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                _ = _commandProcessor.ProcessAsync(entry, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
    }
}
