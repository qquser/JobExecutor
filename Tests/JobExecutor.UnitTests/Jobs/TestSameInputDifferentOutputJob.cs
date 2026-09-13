using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

public sealed class TestSameInputDifferentOutputJob : IJob<TestForEachJobInput, TestOtherJobResult>
{
    public Task DoAsync(TestForEachJobInput input, CancellationToken token) => Task.CompletedTask;

    public TestOtherJobResult GetCurrentState() => new(0);
}

public sealed record TestOtherJobResult(int Data);
