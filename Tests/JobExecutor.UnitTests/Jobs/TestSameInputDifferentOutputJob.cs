using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

public sealed class TestSameInputDifferentOutputJob : IJob<TestForEachJobInput, TestOtherJobResult>
{
    public Task<bool> DoAsync(TestForEachJobInput input, CancellationToken token) => Task.FromResult(true);

    public TestOtherJobResult GetCurrentState() => new(0);
}

public sealed record TestOtherJobResult(int Data);
