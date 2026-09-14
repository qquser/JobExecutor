using JobExecutor.Abstractions.Models;

namespace JobExecutor.BackgroundService.Models;

/// <summary>
/// A job that has been submitted but not yet processed by the engine. It carries the job id,
/// the input to run, and the <see cref="Signals"/> the caller awaits.
/// </summary>
internal sealed record JobRequest<TIn>
    where TIn : class
{
    public required JobId JobId { get; init; }

    public required TIn Input { get; init; }

    public required JobSignals Signals { get; init; }
}
