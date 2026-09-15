using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

public sealed class TestUnresolvableJob : IActiveJob<TestUnresolvableJobInput, TestUnresolvableJobResult>
{
    public TestUnresolvableJob()
    {
        throw new InvalidOperationException("Test dependency could not be resolved.");
    }

    public Task DoAsync(TestUnresolvableJobInput input, CancellationToken token) => Task.CompletedTask;

    public TestUnresolvableJobResult GetCurrentState() => new(0);
}

public sealed record TestUnresolvableJobInput;

public sealed record TestUnresolvableJobResult(int Data);
