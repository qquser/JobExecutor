using System.Threading.Channels;
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.Abstractions.Models;
using JobExecutor.Abstractions.Models.Options;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using JobExecutor.BackgroundService.Processing;
using JobExecutor.BackgroundService.Processing.Runner;
using JobExecutor.BackgroundService.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace JobExecutor.BackgroundService;

public static class ServicesConfiguration
{
    public static IServiceCollection AddBackgroundJobs<TIn, TOut, TJob>(
        this IServiceCollection services,
        Action<JobRetryOptions>? configureRetry = null,
        Action<JobTimeoutOptions>? configureTimeout = null)
        where TIn : class
        where TOut : class
        where TJob : class, IActiveJob<TIn, TOut>
    {
        if (services.Any(descriptor => descriptor.ImplementationType == typeof(TJob)))
        {
            throw new InvalidOperationException(
                $"Background job {typeof(TJob).Name} is already registered. " +
                "Each background job class must be registered only once.");
        }

        if (services.Any(descriptor => descriptor.ServiceType == typeof(Channel<JobRequest<TIn>>)))
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

        services.AddOptions();

        if (configureRetry is not null)
            services.Configure<JobRetryOptions>(configureRetry);

        if (configureTimeout is not null)
            services.Configure<JobTimeoutOptions>(configureTimeout);

        services.AddScoped(typeof(IActiveJob<TIn, TOut>), typeof(TJob));
        services.AddSingleton(_ => Channel.CreateUnbounded<JobRequest<TIn>>());
        services.AddSingleton<IJobRegistry<TIn, TOut>, JobRegistry<TIn, TOut>>();
        services.AddSingleton<IJobEntryFactory<TIn, TOut>, JobEntryFactory<TIn, TOut>>();
        services.AddSingleton<IJobRunner<TIn, TOut>, JobRunner<TIn, TOut>>();
        services.AddSingleton<IJobStateMapper<TIn, TOut>, JobStateMapper<TIn, TOut>>();
        services.AddSingleton<IJobCommandProcessor<TIn, TOut>, JobCommandProcessor<TIn, TOut>>();
        services.AddSingleton<IActiveJobManager<TIn, TOut>, BackgroundJobManager<TIn, TOut>>();
        services.AddHostedService<JobEngine<TIn, TOut>>();
        return services;
    }
}
