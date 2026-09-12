# JobExecutor

A .NET library for running background jobs in-process: submit a job, track its state, retry on failure, and query running jobs — all behind two abstractions (`IJob` and `IJobContext`).

## Features

- **Fire-and-forget or await**: `CreateJobAsync` returns once the job is registered; `DoJobAsync` waits for completion.
- **Retries with backoff**: first attempt plus `maxNrOfRetries`, backoff spread across `minBackoff`..`maxBackoff`.
- **Deduplication**: submitting the same `JobId` twice returns `"<id> job exists."` for the duplicates.
- **Cancellation**: `StopJobAsync` cancels a running job via its `CancellationToken`.
- **Query & pagination**: list all / paginate by creation time / fetch by ids.
- **Result pattern**: no exceptions for control flow — every command returns a `*Result` record.

## Quick start

```csharp
using JobExecutor.Abstractions.Interfaces;
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

    public EmailJobState GetCurrentState(string jobId) => new(jobId, _sent);
}

public sealed record EmailJobInput(IReadOnlyList<string> Recipients);
public sealed record EmailJobState(string Id, int Sent);

// 2. Register the job.
var services = new ServiceCollection();
services.AddBackgroundJobs<EmailJobInput, EmailJobState, EmailJob>();
var provider = services.BuildServiceProvider();

// 3. Start/stop/query through IJobContext.
var context = provider.GetRequiredService<IJobContext<EmailJobInput, EmailJobState>>();

var created = await context.CreateJobAsync("job-1", new EmailJobInput(new[] { "a@x.io" }));
var done    = await context.DoJobAsync("job-2", new EmailJobInput(new[] { "b@x.io" }));
var page    = await context.GetJobsPaginateAsync(skip: 0, take: 10);
```

## Key abstractions

| Abstraction | Role |
|---|---|
| `IJob<TIn, TOut>` | A job: `DoAsync(input, token)` + `GetCurrentState(jobId)`. Registered **scoped**. |
| `IJobContext<TIn, TOut>` | Entry point to create, run, stop, and query jobs. |
| `ServicesConfiguration.AddBackgroundJobs<TIn, TOut, TJob>()` | DI registration (job + hosted engine + registry). |
| `*Result` records | `JobCreatedCommandResult`, `JobDoneCommandResult`, `StopJobCommandResult`, `RespondWorkersInfo<TOut>`, `ReplyWorkerInfo<TOut>`. |

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
