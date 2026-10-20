using System.ComponentModel.DataAnnotations;
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Queries;
using JobExecutor.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace JobExecutor.Api.Controllers;

/// <summary>
/// The standard REST endpoints of a background job type: start, run, stop and query the state.
/// A concrete controller derives from it, adds a route and passes the job manager to the base.
/// </summary>
/// <typeparam name="TIn">The input model of the job.</typeparam>
/// <typeparam name="TOut">The state model of the job.</typeparam>
[ApiController]
public abstract class ActiveJobsController<TIn, TOut>(IActiveJobManager<TIn, TOut> manager) : ControllerBase
    where TIn : class
    where TOut : class
{
    /// <summary>
    /// Starts the job in the background and returns immediately.
    /// </summary>
    [HttpPost("start/{jobId}")]
    [ProducesResponseType(typeof(JobStartedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JobStartedResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobStartedResult>> StartAsync(string jobId, [FromBody] TIn input)
    {
        var result = await manager.StartJobAsync(new JobId(jobId), input, HttpContext.RequestAborted);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Runs the job and waits for it to complete.
    /// </summary>
    [HttpPost("run/{jobId}")]
    [ProducesResponseType(typeof(JobCompletedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JobCompletedResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobCompletedResult>> RunAsync(string jobId, [FromBody] TIn input)
    {
        var result = await manager.RunJobAsync(new JobId(jobId), input, HttpContext.RequestAborted);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Stops the running job.
    /// </summary>
    [HttpPost("stop/{jobId}")]
    [ProducesResponseType(typeof(JobStoppedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JobStoppedResult), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobStoppedResult>> StopAsync(string jobId)
    {
        var result = await manager.StopJobAsync(new JobId(jobId));

        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Gets the state of all active jobs.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ActiveJobsResponse<TOut>>> GetAllAsync()
    {
        var result = await manager.GetAllJobsAsync();

        return Ok(ToResponse(result));
    }

    /// <summary>
    /// Gets the state of the active jobs page by page.
    /// </summary>
    [HttpGet("page")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ActiveJobsResponse<TOut>>> GetPageAsync(
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(0, int.MaxValue)] int take = 10)
    {
        var result = await manager.GetJobsPageAsync(skip, take);

        return Ok(ToResponse(result));
    }

    /// <summary>
    /// Gets the state of the active jobs by their identifiers.
    /// </summary>
    [HttpPost("by-ids")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ActiveJobsResponse<TOut>>> GetByIdsAsync([FromBody] JobIdsRequest request)
    {
        var jobIds = request.JobIds.Select(jobId => new JobId(jobId)).ToArray();

        var result = await manager.GetJobsByIdsAsync(jobIds);

        return Ok(ToResponse(result));
    }

    private static ActiveJobsResponse<TOut> ToResponse(ActiveJobsQueryResult<TOut> result)
    {
        var jobs = result.Jobs
            .Select(job => new JobStateResponse<TOut>(job.Key.Value, job.Value.Result))
            .ToArray();

        return new ActiveJobsResponse<TOut>(result.TotalCount, jobs.Length, jobs);
    }
}
