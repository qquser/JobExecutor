using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

public sealed class TestExceptionOnFirstTryJob : IJob<TestExceptionOnFirstTryJobInput, TestForEachJobResult>
{
    private int _attempts;

    public async Task<bool> DoAsync(TestExceptionOnFirstTryJobInput input, CancellationToken token)
    {
        if (_attempts++ == 0)
            throw new Exception("First attempt failure.");

        foreach (var item in Enumerable.Range(0, input.Count))
        {
            if (token.IsCancellationRequested)
                return false;

            await Task.Delay(10, token);
        }

        return true;
    }

    public TestForEachJobResult GetCurrentState(string jobId) => new(jobId, 0);
}

public sealed record TestExceptionOnFirstTryJobInput(int Count);
