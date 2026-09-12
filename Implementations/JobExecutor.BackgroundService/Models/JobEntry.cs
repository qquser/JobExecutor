using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;

namespace JobExecutor.BackgroundService.Models;

internal sealed class JobEntry<TIn, TOut>
    where TIn : class
    where TOut : class
{
    public required JobRunModel<TIn> Run { get; init; }
    public required bool IsCreateCommand { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required CancellationTokenSource Cts { get; init; }
    public required TaskCompletionSource<JobCreatedCommandResult> Created { get; init; }
    public required TaskCompletionSource<JobDoneCommandResult> Done { get; init; }

    /// <summary>Заполняется движком один раз после резолва <see cref="IJob{TIn,TOut}"/> из DI-scope.</summary>
    public IJob<TIn, TOut>? Job { get; set; }
}
