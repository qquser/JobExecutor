using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

public sealed class TestSameInputDifferentOutputJob : IJob<TestForEachJobInput, TestOtherJobResult>
{
    public Task<bool> DoAsync(TestForEachJobInput input, CancellationToken token) => Task.FromResult(true);

    public TestOtherJobResult GetCurrentState(string jobId) => new(jobId, 0);
}

public sealed record TestOtherJobResult(string Id, int Data);
