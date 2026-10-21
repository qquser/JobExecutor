using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Api.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace JobExecutor.Api.Controllers;

/// <summary>
/// The REST endpoints of the slow background job.
/// </summary>
[Route("api/slow-start-jobs")]
public sealed class SlowStartJobsController(IActiveJobManager<SlowStartJobInput, SlowStartJobState> manager)
    : ActiveJobsController<SlowStartJobInput, SlowStartJobState>(manager);
