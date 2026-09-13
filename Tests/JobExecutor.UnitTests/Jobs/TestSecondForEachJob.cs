using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

public sealed class TestSecondForEachJob : IJob<TestForEachJobInput, TestForEachJobResult>
{
    public Task DoAsync(TestForEachJobInput input, CancellationToken token) => Task.CompletedTask;

    public TestForEachJobResult GetCurrentState() => new(0);
}
