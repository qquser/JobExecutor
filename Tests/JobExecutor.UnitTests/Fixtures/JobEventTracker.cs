using System.Collections.Concurrent;

namespace JobExecutor.UnitTests.Fixtures;

/// <summary>
/// Records the events of the test jobs, so the tests can observe what happened inside a job
/// even after the job itself is gone from the registry.
/// </summary>
public sealed class JobEventTracker
{
    private readonly ConcurrentDictionary<string, int> _counts = new();
    private readonly ConcurrentDictionary<string, string> _lastMessages = new();

    public void Record(string eventName, string message = "")
    {
        _counts.AddOrUpdate(eventName, 1, (_, count) => count + 1);
        _lastMessages[eventName] = message;
    }

    public int CountOf(string eventName)
        => _counts.TryGetValue(eventName, out var count) ? count : 0;

    public string LastMessageOf(string eventName)
        => _lastMessages.TryGetValue(eventName, out var message) ? message : string.Empty;
}
