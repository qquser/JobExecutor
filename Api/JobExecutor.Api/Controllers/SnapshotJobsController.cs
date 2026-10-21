using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Api.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace JobExecutor.Api.Controllers;

/// <summary>
/// The REST endpoints of the snapshot background job.
/// </summary>
[Route("api/snapshot-jobs")]
public sealed class SnapshotJobsController(IActiveJobManager<SnapshotJobInput, SnapshotJobState> manager)
    : ActiveJobsController<SnapshotJobInput, SnapshotJobState>(manager);
