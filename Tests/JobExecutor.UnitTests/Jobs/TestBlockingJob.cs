using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

public sealed class TestBlockingJob : IActiveJob<TestBlockingJobInput, TestBlockingJobResult>
{
    public Task DoAsync(TestBlockingJobInput input, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
            Thread.Sleep(10);

        token.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public TestBlockingJobResult GetCurrentState() => new(0);
}

public sealed record TestBlockingJobInput;

public sealed record TestBlockingJobResult(int Data);
