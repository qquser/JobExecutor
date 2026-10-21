using System.Threading.Channels;
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models.Options;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using JobExecutor.BackgroundService.Processing;
using JobExecutor.BackgroundService.Registry;
using JobExecutor.BackgroundService.Registry.Mapper;
using Microsoft.Extensions.DependencyInjection;

namespace JobExecutor.BackgroundService;

public static class ServicesConfiguration
{
    /// <summary>
    /// Registers a background job with its engine, its registry and its manager.
    /// </summary>
    /// <param name="services">The service collection of the application.</param>
    /// <param name="configureTimeout">The timeouts of the job. The parameter is required, so the
    /// wait for the start of the job and the whole life of the job are always set explicitly.</param>
    public static IServiceCollection AddBackgroundJobs<TIn, TOut, TJob>(
        this IServiceCollection services,
        Action<JobTimeoutOptions> configureTimeout)
        where TIn : class
        where TOut : class
        where TJob : class, IActiveJob<TIn, TOut>
    {
        ArgumentNullException.ThrowIfNull(configureTimeout);

        if (services.Any(descriptor => descriptor.ImplementationType == typeof(TJob)))
        {
            throw new InvalidOperationException(
                $"Background job {typeof(TJob).Name} is already registered. " +
                "Each background job class must be registered only once.");
        }

        if (services.Any(descriptor => descriptor.ServiceType == typeof(Channel<StartJobRequest<TIn>>)))
        {
            throw new InvalidOperationException(
                $"A background job with input type {typeof(TIn).Name} is already registered. " +
                "Each background job must use its own input model type.");
        }

        if (services.Any(descriptor =>
                descriptor.ServiceType is { } type &&
                type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(IActiveJob<,>) &&
                type.GetGenericArguments()[1] == typeof(TOut)))
        {
            throw new InvalidOperationException(
                $"A background job with output type {typeof(TOut).Name} is already registered. " +
                "Each background job must use its own output model type.");
        }

        // The timeouts are validated at the registration, so a broken configuration fails fast
        // and not in the middle of a running job.
        var timeoutOptions = new JobTimeoutOptions();
        configureTimeout(timeoutOptions);
        ValidateTimeoutOptions(timeoutOptions);

        services.Configure<JobTimeoutOptions>(options =>
        {
            options.StartTimeout = timeoutOptions.StartTimeout;
            options.ExecutionTimeout = timeoutOptions.ExecutionTimeout;
        });

        services.AddScoped(typeof(IActiveJob<TIn, TOut>), typeof(TJob));
        services.AddSingleton(_ => Channel.CreateUnbounded<StartJobRequest<TIn>>());
        services.AddSingleton(_ => Channel.CreateUnbounded<RunJobRequest<TIn>>());
        services.AddSingleton<IJobRegistry<TIn, TOut>, JobRegistry<TIn, TOut>>();
        services.AddSingleton<IJobEntryFactory<TIn>, JobEntryFactory<TIn>>();
        services.AddSingleton<IJobStateMapper<TIn, TOut>, JobStateMapper<TIn, TOut>>();
        services.AddSingleton<IStartJobCommandProcessor<TIn, TOut>, StartJobCommandProcessor<TIn, TOut>>();
        services.AddSingleton<IRunJobCommandProcessor<TIn, TOut>, RunJobCommandProcessor<TIn, TOut>>();
        services.AddSingleton<IActiveJobManager<TIn, TOut>, BackgroundJobManager<TIn, TOut>>();
        services.AddHostedService<StartJobEngine<TIn, TOut>>();
        services.AddHostedService<RunJobEngine<TIn, TOut>>();
        return services;
    }

    /// <summary>
    /// The timeouts must be positive and finite: a caller must not wait for a job forever and a
    /// job must not live forever.
    /// </summary>
    private static void ValidateTimeoutOptions(JobTimeoutOptions options)
    {
        if (options.StartTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                $"The start timeout must be a positive, finite time span, but it is {options.StartTimeout}.",
                nameof(options));
        }

        if (options.ExecutionTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                $"The execution timeout must be a positive, finite time span, but it is {options.ExecutionTimeout}.",
                nameof(options));
        }
    }
}
