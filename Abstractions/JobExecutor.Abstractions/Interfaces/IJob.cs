namespace JobExecutor.Abstractions.Interfaces;

/// <summary>
/// A background job.
/// </summary>
/// <typeparam name="TIn">Input parameters for the command that starts the background job.</typeparam>
/// <typeparam name="TOut">The current state of the background job.</typeparam>
public interface IJob<in TIn, out TOut>
    where TIn : class
    where TOut : class
{
    Task<bool> DoAsync(TIn input, CancellationToken token);

    /// <summary>
    /// Describes the state of the fields of the IJob class that are changed by the DoAsync method.
    /// </summary>
    TOut GetCurrentState();
}
