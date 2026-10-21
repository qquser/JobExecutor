using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.UnitTests.Jobs;

/// <summary>
/// The job that registers a throwing callback on its token. The cancellation of the token then
/// fails with an aggregate exception, which must not escape the manager.
/// </summary>
public sealed class TestThrowingCallbackJob : IActiveJob<TestThrowingCallbackJobInput, TestThrowingCallbackJobResult>
{
    public async Task DoAsync(TestThrowingCallbackJobInput input, CancellationToken token)
    {
        // The callback fails the cancellation of the token with an aggregate exception.
        token.Register(() => throw new InvalidOperationException("The cancellation callback failed."));

        await Task.Delay(Timeout.Infinite, token);
    }

    public TestThrowingCallbackJobResult GetCurrentState() => new(0);
}

public sealed record TestThrowingCallbackJobInput;

public sealed record TestThrowingCallbackJobResult(int Data);
