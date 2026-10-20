using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

/// <summary>
/// The job that can wait until it is cancelled, or fail in different ways.
/// </summary>
public sealed class TestCancellableJob : IActiveJob<TestCancellableJobInput, TestCancellableJobResult>
{
    public async Task DoAsync(TestCancellableJobInput input, CancellationToken token)
    {
        if (input.BlockUntilCancelled)
        {
            await Task.Delay(Timeout.Infinite, token);
            return;
        }

        if (input.ThrowOperationCanceledException)
            throw new OperationCanceledException("The operation was canceled.");

        if (input.Throw)
            throw new InvalidOperationException("Test exception.");

        await Task.Delay(input.DelayMs, token);
    }

    public TestCancellableJobResult GetCurrentState() => new(0);
}

public sealed record TestCancellableJobInput
{
    public bool BlockUntilCancelled { get; init; }

    public bool Throw { get; init; }

    public bool ThrowOperationCanceledException { get; init; }

    public int DelayMs { get; init; } = 10;
}

public sealed record TestCancellableJobResult(int Data);
