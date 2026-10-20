# API

A REST API over the `JobExecutor` library. It is a playground for the library: every job registered in
`Program.cs` is exposed through a set of REST endpoints, so the behaviour of the library can be tested
manually (curl, Swagger, the `JobExecutor.Api.http` file) without writing unit tests.

## Run

```bash
dotnet run --project Api/JobExecutor.Api
```

- Swagger UI: `http://localhost:5288/swagger` (available in every environment, the endpoints can be called right from it)
- OpenAPI document: `http://localhost:5288/swagger/v1/swagger.json`
- Health check: `GET http://localhost:5288/health`
- Ready-made requests: `Api/JobExecutor.Api/JobExecutor.Api.http`

## Endpoints

Every job type gets the same set of endpoints. The routes below are the ones of the sample job
(`/api/sample-jobs`); the failing job is exposed under `/api/failing-jobs`.

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/sample-jobs/start/{jobId}` | Starts the job in the background and returns immediately. |
| `POST` | `/api/sample-jobs/run/{jobId}` | Runs the job and waits for it to complete. |
| `POST` | `/api/sample-jobs/stop/{jobId}` | Stops the running job. |
| `GET` | `/api/sample-jobs` | The state of all active jobs. |
| `GET` | `/api/sample-jobs/page?skip=0&take=10` | The state of the active jobs page by page. |
| `POST` | `/api/sample-jobs/by-ids` | The state of the active jobs by their identifiers. |

The body of `start` and `run` is the input model of the job:

```json
{ "steps": 10, "stepDelayMs": 1000 }
```

The body of `by-ids` is `{ "jobIds": [ "job-1" ] }`.

The library returns results instead of throwing exceptions, so the responses look like this:

```json
{ "success": true, "result": "" }
```

`success: false` is returned with `400 Bad Request` (a duplicate job id, a failed job) or
`404 Not Found` (stopping a job that is not running).

The endpoints pass `HttpContext.RequestAborted` to the manager, so a job is cancelled when the
client disconnects.

The query endpoints return the states of the jobs:

```json
{
  "totalCount": 51,
  "count": 3,
  "jobs": [ { "jobId": "job-1", "state": { "completedSteps": 4, "totalSteps": 10 } } ]
}
```

`totalCount` is the total number of the active jobs of the job type, `count` is the number of the
jobs in this response — they differ for `page` and `by-ids`.

`skip` and `take` of the `page` endpoint must not be negative, otherwise the request is rejected
with `400 Bad Request`.

## Logs

In Development the API logs every request — the method, the path, the status code and the
duration:

```
info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
      Request starting HTTP/1.1 GET http://localhost:5288/api/sample-jobs - - -
info: Microsoft.AspNetCore.Hosting.Diagnostics[2]
      Request finished HTTP/1.1 GET http://localhost:5288/api/sample-jobs - 200 - application/json 8.3965ms
```

The internal logs of MVC and of the routing are muted, so only the requests and the problems of
the jobs are in the log. The levels are set in `appsettings.Development.json`.

## Layout

```
Api/JobExecutor.Api/
    Controllers/ActiveJobsController.cs   — the shared endpoints, derived per job type
    Controllers/SampleJobsController.cs   — the routes of the sample job
    Controllers/FailingJobsController.cs  — the routes of the failing job
    Contracts/                            — the request and response models of the API
    Jobs/                                 — the test jobs
    Program.cs                            — the registration of the jobs
```

## Add a test job

1. **The job** — put it in `Jobs/`. A job implements `IActiveJob<TIn, TOut>`, does the work in
   `DoAsync` and reports its progress in `GetCurrentState`:

   ```csharp
   public sealed class MyJob : IActiveJob<MyJobInput, MyJobState>
   {
       public async Task DoAsync(MyJobInput input, CancellationToken token) { /* ... */ }

       public MyJobState GetCurrentState() => new(/* ... */);
   }

   public sealed record MyJobInput { public int Count { get; init; } = 10; }

   public sealed record MyJobState(int Done);
   ```

   Every job must use **its own** input and output model types — the registration fails fast when
   two jobs share a model type.

2. **The controller** — derive from `ActiveJobsController<TIn, TOut>` and add a route:

   ```csharp
   [Route("api/my-jobs")]
   public sealed class MyJobsController : ActiveJobsController<MyJobInput, MyJobState>
   {
       public MyJobsController(IActiveJobManager<MyJobInput, MyJobState> manager) : base(manager) { }
   }
   ```

   All the standard endpoints are inherited, no extra code is needed.

3. **The registration** — add the job to `Program.cs`:

   ```csharp
   builder.Services.AddBackgroundJobs<MyJobInput, MyJobState, MyJob>();
   ```

   The timeout of `start` and `run` can be changed per job:

   ```csharp
   builder.Services.AddBackgroundJobs<MyJobInput, MyJobState, MyJob>(options => options.Timeout = TimeSpan.FromMinutes(5));
   ```

4. **The requests** — add the calls to `JobExecutor.Api.http` to test the job.

## Add a custom endpoint

An endpoint that is specific to a job is added to the controller of that job, next to the inherited ones:

```csharp
[HttpPost("reset/{jobId}")]
public async Task<ActionResult<JobStoppedResult>> ResetAsync(string jobId)
{
    var result = await manager.StopJobAsync(new JobId(jobId));

    return result.Success ? Ok(result) : NotFound(result);
}
```

The manager of the job is available in the base controller as the `manager` parameter of its
primary constructor.
