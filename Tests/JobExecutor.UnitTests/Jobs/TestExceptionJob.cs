using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

public sealed class TestExceptionJob : IJob<TestExceptionJobInput, TestForEachJobResult>
{
    public Task DoAsync(TestExceptionJobInput input, CancellationToken token)
        => throw new Exception("Test exception.");

    public TestForEachJobResult GetCurrentState() => new(0);
}

public sealed record TestExceptionJobInput(int Count);
