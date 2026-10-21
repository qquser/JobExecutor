using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.Api.Jobs;

/// <summary>
/// A job that ignores the cancellation token of the library. The execution timeout cancels the
/// token, but the job keeps working until it finishes on its own. It shows the limit of the
/// timeout: it can cancel the token, but it cannot stop a job that does not observe it.
/// </summary>
public sealed class UncooperativeJob : IActiveJob<UncooperativeJobInput, UncooperativeJobState>
{
    private int _completedSteps;

    public async Task DoAsync(UncooperativeJobInput input, CancellationToken token)
    {
        for (var step = 1; step <= input.Steps; step++)
        {
            await Task.Delay(input.StepDelayMs, CancellationToken.None);

            _completedSteps = step;
        }
    }

    public UncooperativeJobState GetCurrentState() => new(_completedSteps);
}

/// <summary>
/// Input parameters of the uncooperative background job.
/// </summary>
public sealed record UncooperativeJobInput
{
    /// <summary>
    /// The number of steps the job performs.
    /// </summary>
    public int Steps { get; init; } = 5;

    /// <summary>
    /// The delay between the steps, in milliseconds.
    /// </summary>
    public int StepDelayMs { get; init; } = 1000;
}

/// <summary>
/// The current state of the uncooperative background job.
/// </summary>
public sealed record UncooperativeJobState(int CompletedSteps);
