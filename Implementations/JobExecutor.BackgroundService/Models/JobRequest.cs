using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.BackgroundService.Models;

/// <summary>
/// A job that has been submitted but not yet resolved by the engine. It carries the
/// <see cref="Run"/> data and the <see cref="Signals"/> the caller awaits. The engine maps
/// it into a <see cref="JobEntry{TIn,TOut}"/> once it resolves the <see cref="IJob{TIn,TOut}"/>
/// from a DI scope.
/// </summary>
internal sealed record JobRequest<TIn>
    where TIn : class
{
    public required JobRunModel<TIn> Run { get; init; }

    public required JobSignals Signals { get; init; }
}
