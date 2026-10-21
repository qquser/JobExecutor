using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Api.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace JobExecutor.Api.Controllers;

/// <summary>
/// The REST endpoints of the blocking background job.
/// </summary>
[Route("api/blocking-jobs")]
public sealed class BlockingJobsController(IActiveJobManager<BlockingJobInput, BlockingJobState> manager)
    : ActiveJobsController<BlockingJobInput, BlockingJobState>(manager);
