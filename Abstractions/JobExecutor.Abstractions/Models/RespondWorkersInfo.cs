namespace JobExecutor.Abstractions.Models;

public sealed record RespondWorkersInfo<TOut>(
    long RequestId,
    Dictionary<string, ReplyWorkerInfo<TOut>> WorkersData,
    int TotalCount)

    where TOut : class
{
    public RespondWorkersInfo(long requestId) : this(requestId, new Dictionary<string, ReplyWorkerInfo<TOut>>(), 0)
    {
    }
}
