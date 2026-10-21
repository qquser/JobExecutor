using JobExecutor.Abstractions.Models.Options;

namespace JobExecutor.BackgroundService.Interfaces;

/// <summary>
/// The timeouts of one job type. Every job type has its own timeouts, so the registration of one
/// job type never changes the timeouts of another one.
/// </summary>
internal interface IJobTimeouts<TIn, TOut>
    where TIn : class
    where TOut : class
{
    JobTimeoutOptions Value { get; }
}
