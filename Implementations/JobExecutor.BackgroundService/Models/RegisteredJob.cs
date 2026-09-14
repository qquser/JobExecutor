using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.BackgroundService.Models;

/// <summary>
/// A job registered as active: the resolved <see cref="IActiveJob{TIn,TOut}"/> instance used to read
/// its current state, and the per-job <see cref="CancellationTokenSource"/> used to stop it.
/// </summary>
internal sealed record RegisteredJob<TIn, TOut>(IActiveJob<TIn, TOut> Job, CancellationTokenSource Cts)
    where TIn : class
    where TOut : class;
