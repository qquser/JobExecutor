namespace JobExecutor.Abstractions.Models;

public sealed record ReplyWorkerInfo<TOut>(bool Success, string ErrorMessage)
    where TOut : class
{
    public ReplyWorkerInfo(TOut result) : this(true, string.Empty)
    {
        Result = result;
    }

    public TOut? Result { get; }
}
