using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.Benchmarks.Jobs;

public sealed class NoOpJob : IJob<NoOpJobInput, NoOpJobResult>
{
    public Task DoAsync(NoOpJobInput input, CancellationToken token)
        => Task.CompletedTask;

    public NoOpJobResult GetCurrentState() => new();
}

public sealed record NoOpJobInput;

public sealed record NoOpJobResult;
