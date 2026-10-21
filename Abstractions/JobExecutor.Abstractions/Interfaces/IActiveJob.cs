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
    /// Returns the current state of the job. The state is read from another thread while the job
    /// is working, so it must be one immutable snapshot that is published atomically — for
    /// example with <c>Volatile.Write</c> and <c>Volatile.Read</c>. A job that returns its fields
    /// one by one shows the caller a mixture of the steps, because the getter cannot be made
    /// atomic from the outside.
    /// </summary>
    TOut GetCurrentState();
}
