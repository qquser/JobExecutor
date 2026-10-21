using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Api.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace JobExecutor.Api.Controllers;

/// <summary>
/// The REST endpoints of the uncooperative background job.
/// </summary>
[Route("api/uncooperative-jobs")]
public sealed class UncooperativeJobsController(IActiveJobManager<UncooperativeJobInput, UncooperativeJobState> manager)
    : ActiveJobsController<UncooperativeJobInput, UncooperativeJobState>(manager);
