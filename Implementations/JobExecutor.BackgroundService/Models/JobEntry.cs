using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.BackgroundService.Models;

/// <summary>
/// A resolved job: the <see cref="Run"/> data, the caller's <see cref="Signals"/>, and the
/// <see cref="Job"/> instance resolved from the DI scope. Built once by the engine and never mutated.
/// </summary>
internal sealed class JobEntry<TIn, TOut>
    where TIn : class
    where TOut : class
{
    public required JobRunModel<TIn> Run { get; init; }

    public required JobSignals Signals { get; init; }

    public required IJob<TIn, TOut> Job { get; init; }
}
