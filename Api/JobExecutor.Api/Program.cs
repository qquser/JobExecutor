using System.Reflection;
using JobExecutor.Api.Jobs;
using JobExecutor.BackgroundService;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "JobExecutor API",
        Version = "v1",
        Description = "REST API for testing the JobExecutor library.",
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

builder.Services.AddBackgroundJobs<SampleJobInput, SampleJobState, SampleJob>(options =>
{
    options.StartTimeout = TimeSpan.FromMinutes(2);
    options.ExecutionTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddBackgroundJobs<FailingJobInput, FailingJobState, FailingJob>(options =>
{
    options.StartTimeout = TimeSpan.FromMinutes(2);
    options.ExecutionTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddBackgroundJobs<UncooperativeJobInput, UncooperativeJobState, UncooperativeJob>(options =>
{
    options.StartTimeout = TimeSpan.FromMinutes(2);
    options.ExecutionTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddBackgroundJobs<BlockingJobInput, BlockingJobState, BlockingJob>(options =>
{
    options.StartTimeout = TimeSpan.FromMinutes(2);
    options.ExecutionTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddBackgroundJobs<ExplodingJobInput, ExplodingJobState, ExplodingJob>(options =>
{
    options.StartTimeout = TimeSpan.FromMinutes(2);
    options.ExecutionTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddBackgroundJobs<SlowStartJobInput, SlowStartJobState, SlowStartJob>(options =>
{
    options.StartTimeout = TimeSpan.FromMinutes(2);
    options.ExecutionTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddBackgroundJobs<InfiniteJobInput, InfiniteJobState, InfiniteJob>(options =>
{
    options.StartTimeout = TimeSpan.FromMinutes(2);
    options.ExecutionTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddBackgroundJobs<MutatingStateJobInput, MutatingStateJobState, MutatingStateJob>(options =>
{
    options.StartTimeout = TimeSpan.FromMinutes(2);
    options.ExecutionTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddBackgroundJobs<SnapshotJobInput, SnapshotJobState, SnapshotJob>(options =>
{
    options.StartTimeout = TimeSpan.FromMinutes(2);
    options.ExecutionTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.Configure<SlowStartJobOptions>(options =>
{
    options.ResolutionDelay = TimeSpan.FromSeconds(2);
});

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "JobExecutor API v1");
    options.DocumentTitle = "JobExecutor API";
});

app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
