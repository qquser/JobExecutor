using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.Api.Jobs;

/// <summary>
/// The ways the exploding background job can fail.
/// </summary>
public enum ExplodingJobFailure
{
    /// <summary>
    /// The job throws an ordinary exception.
    /// </summary>
    Plain,

    /// <summary>
    /// The job throws <see cref="OperationCanceledException"/> without the token being cancelled.
    /// </summary>
    OperationCanceled,

    /// <summary>
    /// The job throws an exception with an empty message.
    /// </summary>
    EmptyMessage,

    /// <summary>
    /// The job throws an aggregate exception with several failures.
    /// </summary>
    Aggregate,
}

/// <summary>
/// A job that fails in a configurable way. It shows what the caller receives for every kind of
/// failure and how the failure of the job is distinguished from the cancellation of the job.
/// </summary>
public sealed class ExplodingJob : IActiveJob<ExplodingJobInput, ExplodingJobState>
{
    private int _attempts;

    public async Task DoAsync(ExplodingJobInput input, CancellationToken token)
    {
        _attempts++;

        await Task.Delay(input.DelayMs, token);

        switch (input.Failure)
        {
            case ExplodingJobFailure.Plain:
                throw new InvalidOperationException(input.Message);
            case ExplodingJobFailure.OperationCanceled:
                throw new OperationCanceledException(input.Message);
            case ExplodingJobFailure.EmptyMessage:
                throw new InvalidOperationException(string.Empty);
            case ExplodingJobFailure.Aggregate:
                throw new AggregateException([
                    new InvalidOperationException("First failure."),
                    new InvalidOperationException("Second failure."),
                ]);
            default:
                throw new InvalidOperationException(input.Message);
        }
    }

    public ExplodingJobState GetCurrentState() => new(_attempts);
}

/// <summary>
/// Input parameters of the exploding background job.
/// </summary>
public sealed record ExplodingJobInput
{
    /// <summary>
    /// The way the job fails.
    /// </summary>
    public ExplodingJobFailure Failure { get; init; } = ExplodingJobFailure.Plain;

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
/// The current state of the exploding background job.
/// </summary>
public sealed record ExplodingJobState(int Attempts);
