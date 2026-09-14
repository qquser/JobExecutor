using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

public sealed class TestOtherJob : IActiveJob<TestOtherJobInput, TestOtherJobResult>
{
    public Task DoAsync(TestOtherJobInput input, CancellationToken token) => Task.CompletedTask;

    public TestOtherJobResult GetCurrentState() => new(0);
}

public sealed record TestOtherJobInput;
