using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

public sealed class TestForEachJob : IActiveJob<TestForEachJobInput, TestForEachJobResult>
{
    private int _currentState;

    public async Task DoAsync(TestForEachJobInput input, CancellationToken token)
    {
        foreach (var item in Enumerable.Range(0, input.Count))
        {
            token.ThrowIfCancellationRequested();

            _currentState = item;
            await Task.Delay(10, token);
        }
    }

    public TestForEachJobResult GetCurrentState() => new(_currentState);
}

public sealed record TestForEachJobInput(int Count);

public sealed record TestForEachJobResult(int Data);
