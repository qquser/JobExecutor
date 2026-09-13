# JobExecutor

A .NET library for running background jobs in-process: submit a job, track its state, retry on failure, and query running jobs — all behind two abstractions (`IJob` and `IJobManager`).

## Features

- **Start-and-forget or await**: `StartJobAsync` returns once the job has started and keeps running in the background (you can still reach it by `JobId`); `RunJobAsync` waits for completion.
- **Retries with backoff**: first attempt plus `maxNrOfRetries`, backoff spread across `minBackoff`..`maxBackoff`.
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
public sealed class EmailJob : IJob<EmailJobInput, EmailJobState>
{
    private int _sent;

    public async Task<bool> DoAsync(EmailJobInput input, CancellationToken token)
    {
        foreach (var recipient in input.Recipients)
        {
            if (token.IsCancellationRequested)
                return false;

            await SendAsync(recipient, token);
            _sent++;
        }

        return true;
    }

    public EmailJobState GetCurrentState() => new(_sent);
}

public sealed record EmailJobInput(IReadOnlyList<string> Recipients);
public sealed record EmailJobState(int Sent);

// 2. Register the job.
var services = new ServiceCollection();
services.AddBackgroundJobs<EmailJobInput, EmailJobState, EmailJob>();
var provider = services.BuildServiceProvider();

// 3. Start/run/stop/query through IJobManager.
var manager = provider.GetRequiredService<IJobManager<EmailJobInput, EmailJobState>>();

var started = await manager.StartJobAsync(new JobId("job-1"), new EmailJobInput(new[] { "a@x.io" }));
var done    = await manager.RunJobAsync(new JobId("job-2"), new EmailJobInput(new[] { "b@x.io" }));
var page    = await manager.GetJobsPageAsync(skip: 0, take: 10);
```

## Key abstractions

| Abstraction | Role |
|---|---|
| `IJob<TIn, TOut>` | A job: `DoAsync(input, token)` + `GetCurrentState()`. Registered **scoped**. |
| `IJobManager<TIn, TOut>` | Entry point to start, run, stop, and query jobs. |
| `ServicesConfiguration.AddBackgroundJobs<TIn, TOut, TJob>()` | DI registration (job + hosted engine + registry). Each job must use its **own input and output model types** (`TIn`, `TOut`) — reusing either model for another job fails fast. |
| Result & state records | `JobStartedResult`, `JobCompletedResult`, `JobStoppedResult`, `JobsQueryResult<TOut>`, `JobStateInfo<TOut>`. |

## Projects

```
Abstractions/     JobExecutor.Abstractions      — interfaces + result models (no NuGet deps)
Implementations/  JobExecutor.BackgroundService — the engine, registry, runner
Tests/            JobExecutor.UnitTests         — xUnit tests (fakes, in-memory DI)
Benchmarks/       JobExecutor.Benchmarks        — BenchmarkDotNet console app
```

## Build, test, benchmark

```bash
dotnet build JobExecutor.sln
dotnet test  JobExecutor.sln
dotnet run -c Release --project Benchmarks/JobExecutor.Benchmarks -- --filter "*"
```
