using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.Api.Jobs;

/// <summary>
/// A sample background job that always fails. It is used to test how errors are returned
/// by the library and how they are mapped to the API responses.
/// </summary>
public sealed class FailingJob : IActiveJob<FailingJobInput, FailingJobState>
{
    private int _attempts;

    public async Task DoAsync(FailingJobInput input, CancellationToken token)
    {
        _attempts++;

        await Task.Delay(input.DelayMs, token);

        throw new InvalidOperationException(input.Message);
    }

    public FailingJobState GetCurrentState() => new(_attempts);
}

/// <summary>
/// Input parameters of the failing background job.
/// </summary>
public sealed record FailingJobInput
{
    /// <summary>
    /// The message of the exception the job throws.
    /// </summary>
    public string Message { get; init; } = "Test failure.";

    /// <summary>
    /// The delay before the job fails, in milliseconds.
    /// </summary>
    public int DelayMs { get; init; } = 100;
}

/// <summary>
/// The current state of the failing background job.
/// </summary>
public sealed record FailingJobState(int Attempts);
