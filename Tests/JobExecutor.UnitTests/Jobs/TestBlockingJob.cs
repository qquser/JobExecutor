using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

/// <summary>
/// The job that blocks the engine until it is cancelled, so the next request waits in the queue.
/// The job can also work for a while and finish on its own.
/// </summary>
public sealed class TestBlockingJob : IActiveJob<TestBlockingJobInput, TestBlockingJobResult>
{
    public Task DoAsync(TestBlockingJobInput input, CancellationToken token)
    {
        if (!input.BlockUntilCancelled)
            return WorkAsync(input.WorkMs, token);

        while (!token.IsCancellationRequested)
            Thread.Sleep(10);

        token.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static async Task WorkAsync(int workMs, CancellationToken token)
        => await Task.Delay(workMs, token);

    public TestBlockingJobResult GetCurrentState() => new(0);
}

public sealed record TestBlockingJobInput(bool BlockUntilCancelled = true, int WorkMs = 0);

public sealed record TestBlockingJobResult(int Data);
