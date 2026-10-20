using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Api.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace JobExecutor.Api.Controllers;

/// <summary>
/// The REST endpoints of the sample background job.
/// </summary>
[Route("api/sample-jobs")]
public sealed class SampleJobsController(IActiveJobManager<SampleJobInput, SampleJobState> manager)
    : ActiveJobsController<SampleJobInput, SampleJobState>(manager);
