using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Api.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace JobExecutor.Api.Controllers;

/// <summary>
/// The REST endpoints of the failing background job.
/// </summary>
[Route("api/failing-jobs")]
public sealed class FailingJobsController(IActiveJobManager<FailingJobInput, FailingJobState> manager)
    : ActiveJobsController<FailingJobInput, FailingJobState>(manager);
