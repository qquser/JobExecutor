using JobExecutor.Abstractions.Models.Options;
using Xunit;

namespace JobExecutor.UnitTests;

/// <summary>
/// The options of the timeouts: the wait for the start of the job and the whole life of the job
/// are two separate parameters.
/// </summary>
public class JobTimeoutOptionsTests
{
    [Fact]
    public void JobTimeoutOptions_ShouldUseTwoMinutesAsTheStartTimeout_ByDefault()
    {
        var options = new JobTimeoutOptions();

        Assert.Equal(TimeSpan.FromMinutes(2), options.StartTimeout);
    }

    [Fact]
    public void JobTimeoutOptions_ShouldUseThirtyMinutesAsTheExecutionTimeout_ByDefault()
    {
        var options = new JobTimeoutOptions();

        Assert.Equal(TimeSpan.FromMinutes(30), options.ExecutionTimeout);
    }

    /// <summary>
    /// The two timeouts are independent: the wait for the start of the job and the life of the job
    /// are configured separately.
    /// </summary>
    [Fact]
    public void JobTimeoutOptions_ShouldKeepTheTimeoutsIndependent_WhenOneOfThemIsChanged()
    {
        var options = new JobTimeoutOptions
        {
            StartTimeout = TimeSpan.FromSeconds(30),
        };

        Assert.Equal(TimeSpan.FromSeconds(30), options.StartTimeout);
        Assert.Equal(TimeSpan.FromMinutes(30), options.ExecutionTimeout);

        options.ExecutionTimeout = TimeSpan.FromHours(1);

        Assert.Equal(TimeSpan.FromSeconds(30), options.StartTimeout);
        Assert.Equal(TimeSpan.FromHours(1), options.ExecutionTimeout);
    }

    /// <summary>
    /// The options hold exactly the two timeouts of a job.
    /// </summary>
    [Fact]
    public void JobTimeoutOptions_ShouldContainBothTimeouts()
    {
        var properties = typeof(JobTimeoutOptions)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Contains(nameof(JobTimeoutOptions.StartTimeout), properties);
        Assert.Contains(nameof(JobTimeoutOptions.ExecutionTimeout), properties);
        Assert.Equal(2, properties.Length);
    }
}
