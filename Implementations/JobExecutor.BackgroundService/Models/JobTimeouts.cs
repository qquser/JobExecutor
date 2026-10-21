using JobExecutor.Abstractions.Models.Options;
using JobExecutor.BackgroundService.Interfaces;

namespace JobExecutor.BackgroundService.Models;

/// <summary>
/// The timeouts of one job type. They are set at the registration and never change.
/// </summary>
internal sealed class JobTimeouts<TIn, TOut> : IJobTimeouts<TIn, TOut>
    where TIn : class
    where TOut : class
{
    public JobTimeouts(JobTimeoutOptions value)
    {
        Value = value;
    }

    public JobTimeoutOptions Value { get; }
}
