using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

public sealed class TestSecondForEachJob : IJob<TestForEachJobInput, TestForEachJobResult>
{
    public Task<bool> DoAsync(TestForEachJobInput input, CancellationToken token) => Task.FromResult(true);

    public TestForEachJobResult GetCurrentState() => new(0);
}
