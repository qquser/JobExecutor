using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Api.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace JobExecutor.Api.Controllers;

/// <summary>
/// The REST endpoints of the exploding background job.
/// </summary>
[Route("api/exploding-jobs")]
public sealed class ExplodingJobsController(IActiveJobManager<ExplodingJobInput, ExplodingJobState> manager)
    : ActiveJobsController<ExplodingJobInput, ExplodingJobState>(manager);
