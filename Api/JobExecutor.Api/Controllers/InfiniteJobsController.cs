using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Api.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace JobExecutor.Api.Controllers;

/// <summary>
/// The REST endpoints of the infinite background job.
/// </summary>
[Route("api/infinite-jobs")]
public sealed class InfiniteJobsController(IActiveJobManager<InfiniteJobInput, InfiniteJobState> manager)
    : ActiveJobsController<InfiniteJobInput, InfiniteJobState>(manager);
