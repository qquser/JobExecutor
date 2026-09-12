using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

public sealed class TestForEachJob : IJob<TestForEachJobInput, TestForEachJobResult>
{
    private int _currentState;

    public async Task<bool> DoAsync(TestForEachJobInput input, CancellationToken token)
    {
        foreach (var item in Enumerable.Range(0, input.Count))
        {
            if (token.IsCancellationRequested)
                return false;

            _currentState = item;
            await Task.Delay(10, token);
        }

        return true;
    }

    public TestForEachJobResult GetCurrentState(string jobId) => new(jobId, _currentState);
}

public sealed record TestForEachJobInput(int Count);

public sealed record TestForEachJobResult(string Id, int Data);
