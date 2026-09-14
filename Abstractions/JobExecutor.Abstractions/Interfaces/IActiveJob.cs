namespace JobExecutor.Abstractions.Interfaces;

/// <summary>
/// A background job while it is active. It only exists while its work is running:
/// once the job completes or is stopped, its state is no longer available.
/// </summary>
/// <typeparam name="TIn">Input parameters for the command that starts the background job.</typeparam>
/// <typeparam name="TOut">The current state of the background job.</typeparam>
public interface IActiveJob<in TIn, out TOut>
    where TIn : class
    where TOut : class
{
    Task DoAsync(TIn input, CancellationToken token);

    /// <summary>
    /// Describes the state of the fields of the IActiveJob class that are changed by the DoAsync method.
    /// </summary>
    TOut GetCurrentState();
}
