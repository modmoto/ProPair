using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using ProPair.Adapters.Mongo.Jobs;
using ProPair.Application.Ports;

namespace ProPair.Adapters.Mongo;

public static class DependencyInjection
{
    public static IServiceCollection AddMongo(this IServiceCollection services, MongoOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<MongoContext>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITournamentRepository, TournamentRepository>();
        services.AddScoped<IPreparationRepository, PreparationRepository>();
        services.AddScoped<IMatrixRepository, MatrixRepository>();
        services.AddScoped<IPairingSessionRepository, PairingSessionRepository>();
        return services;
    }

    /// <summary>Lets the API hand work to the sync process via the shared job storage.</summary>
    public static IServiceCollection AddMongoSyncQueue(this IServiceCollection services, MongoOptions options)
    {
        services.AddSingleton<JobStorage>(_ => ProPairJobStorage.Create(options));
        services.AddSingleton<IBackgroundJobClient>(sp => new BackgroundJobClient(sp.GetRequiredService<JobStorage>()));
        services.AddSingleton<ISyncQueue, HangfireSyncQueue>();
        return services;
    }

    public static Task EnsureMongoIndexesAsync(this IServiceProvider services) =>
        services.GetRequiredService<MongoContext>().EnsureIndexesAsync();
}
