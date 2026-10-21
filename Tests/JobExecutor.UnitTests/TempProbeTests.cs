using Xunit;

namespace JobExecutor.UnitTests;

public class TempProbeTests
{
    [Fact]
    public void Probe()
    {
        var lines = new List<string>();

        void Probe(string name, Action action)
        {
            try
            {
                action();
                lines.Add($"{name}: OK");
            }
            catch (Exception ex)
            {
                lines.Add($"{name}: {ex.GetType().Name} - {ex.Message}");
            }
        }

        foreach (var days in new[] { 24, 30, 49, 50, 60, 365 })
        {
            var span = TimeSpan.FromDays(days);

            Probe($"Task.Delay({days}d)", () => Task.Delay(span).GetAwaiter().GetResult());
            Probe($"Task.WaitAsync({days}d)", () => Task.CompletedTask.WaitAsync(span).GetAwaiter().GetResult());
            Probe($"CTS({days}d)", () => { using var c = new CancellationTokenSource(span); });
            Probe($"Timer dueTime({days}d)", () => { using var t = new Timer(_ => { }, null, span, Timeout.InfiniteTimeSpan); });
            Probe($"PeriodicTimer({days}d)", () => { using var t = new PeriodicTimer(span); });
        }

        Probe("Task.Delay(infinite)", () => Task.Delay(Timeout.InfiniteTimeSpan).GetAwaiter().GetResult());
        Probe("Task.WaitAsync(infinite)", () => Task.CompletedTask.WaitAsync(Timeout.InfiniteTimeSpan).GetAwaiter().GetResult());
        Probe("CTS(infinite)", () => { using var c = new CancellationTokenSource(Timeout.InfiniteTimeSpan); });
        Probe("Timer dueTime(infinite)", () => { using var t = new Timer(_ => { }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan); });
        Probe("Thread.Sleep(30d)", () => Thread.Sleep(TimeSpan.FromDays(30)));

        lines.Add($"uint.MaxValue-1 ms = {TimeSpan.FromMilliseconds(uint.MaxValue - 1).TotalDays:F2} days");
        lines.Add($"int.MaxValue ms = {TimeSpan.FromMilliseconds(int.MaxValue).TotalDays:F2} days");
        lines.Add($"Environment.TickCount64 wraps after ~{TimeSpan.FromMilliseconds(long.MaxValue).TotalDays:F0} days");

        File.WriteAllLines("/tmp/probe2.txt", lines);
    }
}
