namespace JobExecutor.BackgroundService.Models;

/// <summary>
/// Неизменяемые данные, которые нужны <see cref="IJobRunner{TIn,TOut}"/> для запуска задания:
/// идентификатор, входные данные для <see cref="IJob{TIn,TOut}.DoAsync"/> и политика повторов.
/// </summary>
internal sealed record JobRunModel<TIn>
    where TIn : class
{
    public required string JobId { get; init; }
    public required TIn Input { get; init; }
    public required int MaxNrOfRetries { get; init; }
    public required TimeSpan Backoff { get; init; }
}
