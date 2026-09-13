using System.Threading.Channels;
using JobExecutor.Abstractions.Interfaces;
using JobExecutor.BackgroundService.Interfaces;
using JobExecutor.BackgroundService.Models;
using JobExecutor.BackgroundService.Processing;
using JobExecutor.BackgroundService.Processing.Runner;
using JobExecutor.BackgroundService.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace JobExecutor.BackgroundService;

public static class ServicesConfiguration
{
    public static IServiceCollection AddBackgroundJobs<TIn, TOut, TJob>(this IServiceCollection services)
        where TIn : class
        where TOut : class
        where TJob : class, IJob<TIn, TOut>
    {
        services.AddScoped(typeof(IJob<TIn, TOut>), typeof(TJob));
        services.AddSingleton(_ => Channel.CreateUnbounded<JobRequest<TIn>>());
        services.AddSingleton<IJobRegistry<TIn, TOut>, JobRegistry<TIn, TOut>>();
        services.AddSingleton<IJobEntryFactory<TIn, TOut>, JobEntryFactory<TIn, TOut>>();
        services.AddSingleton<IJobRunner<TIn, TOut>, JobRunner<TIn, TOut>>();
        services.AddSingleton<IJobStateMapper<TIn, TOut>, JobStateMapper<TIn, TOut>>();
        services.AddSingleton<IJobCommandProcessor<TIn, TOut>, JobCommandProcessor<TIn, TOut>>();
        services.AddSingleton<IJobManager<TIn, TOut>, BackgroundJobManager<TIn, TOut>>();
        services.AddHostedService<JobEngine<TIn, TOut>>();
        return services;
    }
}
