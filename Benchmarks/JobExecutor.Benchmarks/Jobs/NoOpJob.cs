using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.Benchmarks.Jobs;

public sealed class NoOpJob : IJob<NoOpJobInput, NoOpJobResult>
{
    public Task<bool> DoAsync(NoOpJobInput input, CancellationToken token)
        => Task.FromResult(true);

    public NoOpJobResult GetCurrentState(string jobId) => new(jobId);
}

public sealed record NoOpJobInput;

public sealed record NoOpJobResult(string Id);
