# JobExecutor

A .NET library for running background jobs in-process: submit a job, track its state, and query running jobs — all behind two abstractions (`IActiveJob` and `IActiveJobManager`).

## Features

- **Start-and-forget or await**: `StartJobAsync` returns once the job has started and keeps running in the background (you can still reach it by `JobId`); `RunJobAsync` waits for completion.
- **Deduplication**: submitting the same `JobId` twice returns `"<id> job exists."` for the duplicates.
- **Cancellation**: `StopJobAsync` cancels a running job via its `CancellationToken`.
- **Query & pagination**: list all / page by start time / fetch by ids.
- **Result pattern**: no exceptions for control flow — every command returns a `*Result` record.
- **Zero dependencies**: built on the .NET base class library only (async/await, `Task`, `CancellationToken`) — no third-party libraries, no external services, no frameworks. It just runs in-process.

## Quick start

```csharp
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.BackgroundService;
using Microsoft.Extensions.DependencyInjection;

// 1. Implement a job (state lives in instance fields).
public sealed class EmailJob : IActiveJob<EmailJobInput, EmailJobState>
{
    private int _sent;

    public async Task DoAsync(EmailJobInput input, CancellationToken token)
    {
        foreach (var recipient in input.Recipients)
        {
            token.ThrowIfCancellationRequested();

            await SendAsync(recipient, token);
            _sent++;
        }
    }

    public EmailJobState GetCurrentState() => new(_sent);
}

public sealed record EmailJobInput(IReadOnlyList<string> Recipients);
public sealed record EmailJobState(int Sent);

// 2. Register the job.
var services = new ServiceCollection();
services.AddBackgroundJobs<EmailJobInput, EmailJobState, EmailJob>();
var provider = services.BuildServiceProvider();

// 3. Start/run/stop/query through IActiveJobManager.
var manager = provider.GetRequiredService<IActiveJobManager<EmailJobInput, EmailJobState>>();

var started = await manager.StartJobAsync(new JobId("job-1"), new EmailJobInput(new[] { "a@x.io" }));
var done    = await manager.RunJobAsync(new JobId("job-2"), new EmailJobInput(new[] { "b@x.io" }));
var page    = await manager.GetJobsPageAsync(skip: 0, take: 10);
```

## Key abstractions

| Abstraction | Role |
|---|---|
| `IActiveJob<TIn, TOut>` | An active job: `DoAsync(input, token)` + `GetCurrentState()`. Registered **scoped**. |
| `IActiveJobManager<TIn, TOut>` | Entry point to start, run, stop, and query active jobs. |
| `ServicesConfiguration.AddBackgroundJobs<TIn, TOut, TJob>()` | DI registration (job + hosted engine + registry). Each job must use its **own input and output model types** (`TIn`, `TOut`) — reusing either model for another job fails fast. |
| Result & state records | `JobStartedResult`, `JobCompletedResult`, `JobStoppedResult`, `ActiveJobsQueryResult<TOut>`, `ActiveJobStateInfo<TOut>`. |

> **You implement `IActiveJob<TIn, TOut>`; the library provides `IActiveJobManager<TIn, TOut>`.**
> Resolve the manager from DI (`GetRequiredService<...>`) and call it — you never implement it yourself.

## Nested jobs

A job can call another job through the manager of the other job. Pass the token of the calling job,
so the nested job is stopped together with its caller:

```csharp
public sealed class ReportJob(IActiveJobManager<EmailJobInput, EmailJobState> emails) : IActiveJob<ReportInput, ReportState>
{
    public async Task DoAsync(ReportInput input, CancellationToken token)
    {
        // The token is propagated: stopping ReportJob stops EmailJob too.
        var result = await emails.StartJobAsync(new JobId(input.EmailJobId), input.Email, token);

        if (!result.Success)
            throw new InvalidOperationException(result.Result);
    }

    public ReportState GetCurrentState() => new(0);
}
```

Without the token the nested job is independent: it keeps running when the caller is stopped or
times out. The failure of the nested job is returned in its result — the caller decides what to do
with it. The token also interrupts the wait of the nested call, so the whole chain unwinds when the
outermost job is cancelled.

## Projects

```
Abstractions/     JobExecutor.Abstractions      — interfaces + result models (no NuGet deps)
Implementations/  JobExecutor.BackgroundService — the engine, registry, runner
Api/              JobExecutor.Api               — REST API for testing the library
Tests/            JobExecutor.UnitTests         — xUnit tests (fakes, in-memory DI)
Benchmarks/       JobExecutor.Benchmarks        — BenchmarkDotNet console app
```

## Build, test, benchmark

```bash
dotnet build JobExecutor.sln
dotnet test  JobExecutor.sln
dotnet run -c Release --project Benchmarks/JobExecutor.Benchmarks -- --filter "*"
dotnet run --project Api/JobExecutor.Api
```

The API comes up with the Swagger UI at `http://localhost:5288/swagger` — every registered job is
exposed there as `start` / `run` / `stop` / query endpoints. See `Api/README.md` for the details.
