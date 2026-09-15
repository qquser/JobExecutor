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
    public static IServiceCollection AddBackgroundJobs<TIn, TOut, TJob>(
        this IServiceCollection services,
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

        services.AddOptions();

        if (configureTimeout is not null)
            services.Configure<JobTimeoutOptions>(configureTimeout);

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
}
