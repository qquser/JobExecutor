using System.Reflection;
using JobExecutor.Api.Jobs;
using JobExecutor.BackgroundService;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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

// Register the background jobs that are exposed through the API.
// Each job must use its own input and output model types.
// The timeouts are mandatory: the wait for the start of the job and the whole life of the job
// are always set explicitly.
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

var app = builder.Build();

// Configure the HTTP request pipeline.

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
