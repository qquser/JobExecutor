using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

public sealed class TestExceptionOnFirstTryJob : IJob<TestExceptionOnFirstTryJobInput, TestExceptionOnFirstTryJobResult>
{
    private int _attempts;

    public async Task DoAsync(TestExceptionOnFirstTryJobInput input, CancellationToken token)
    {
        if (_attempts++ == 0)
            throw new Exception("First attempt failure.");

        foreach (var item in Enumerable.Range(0, input.Count))
        {
            token.ThrowIfCancellationRequested();

            await Task.Delay(10, token);
        }
    }

    public TestExceptionOnFirstTryJobResult GetCurrentState() => new(0);
}

public sealed record TestExceptionOnFirstTryJobInput(int Count);

public sealed record TestExceptionOnFirstTryJobResult(int Data);
