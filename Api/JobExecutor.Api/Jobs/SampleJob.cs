using JobExecutor.Abstractions.Interfaces;

namespace JobExecutor.Api.Jobs;

/// <summary>
/// A sample background job that performs a number of steps with a delay between them.
/// It is used to test the library through the REST API.
/// </summary>
public sealed class SampleJob : IActiveJob<SampleJobInput, SampleJobState>
{
    private int _completedSteps;
    private int _totalSteps;

    public async Task DoAsync(SampleJobInput input, CancellationToken token)
    {
        _totalSteps = input.Steps;

        for (var step = 1; step <= input.Steps; step++)
        {
            token.ThrowIfCancellationRequested();

            await Task.Delay(input.StepDelayMs, token);

            _completedSteps = step;
        }
    }

    public SampleJobState GetCurrentState() => new(_completedSteps, _totalSteps);
}

/// <summary>
/// Input parameters of the sample background job.
/// </summary>
public sealed record SampleJobInput
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
/// The current state of the sample background job.
/// </summary>
public sealed record SampleJobState(int CompletedSteps, int TotalSteps);
