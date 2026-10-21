using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Api.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace JobExecutor.Api.Controllers;

/// <summary>
/// The REST endpoints of the mutating background job.
/// </summary>
[Route("api/mutating-state-jobs")]
public sealed class MutatingStateJobsController(IActiveJobManager<MutatingStateJobInput, MutatingStateJobState> manager)
    : ActiveJobsController<MutatingStateJobInput, MutatingStateJobState>(manager);
