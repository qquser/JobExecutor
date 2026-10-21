using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.Api.Jobs;

/// <summary>
/// A job that changes several fields of its state one after another. The state is read by another
/// thread while the job is working, so a reader can catch the moment when the fields belong to
/// different steps. It shows that the live state is not a consistent snapshot.
/// </summary>
public sealed class MutatingStateJob : IActiveJob<MutatingStateJobInput, MutatingStateJobState>
{
    private int _completedSteps;
    private int _doubledSteps;
    private string _label = string.Empty;

    public async Task DoAsync(MutatingStateJobInput input, CancellationToken token)
    {
        for (var step = 1; step <= input.Steps; step++)
        {
            await Task.Delay(input.StepDelayMs, token);

            _completedSteps = step;
            _doubledSteps = step * 2;
            _label = $"step-{step}";
        }
    }

    public MutatingStateJobState GetCurrentState() => new(_completedSteps, _doubledSteps, _label);
}

/// <summary>
/// Input parameters of the mutating background job.
/// </summary>
public sealed record MutatingStateJobInput
{
    /// <summary>
    /// The number of steps the job performs.
    /// </summary>
    public int Steps { get; init; } = 20;

    /// <summary>
    /// The delay between the steps, in milliseconds.
    /// </summary>
    public int StepDelayMs { get; init; } = 100;
}

/// <summary>
/// The current state of the mutating background job.
/// </summary>
public sealed record MutatingStateJobState(int CompletedSteps, int DoubledSteps, string Label);
