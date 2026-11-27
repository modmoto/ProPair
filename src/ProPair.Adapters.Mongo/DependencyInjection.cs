using Microsoft.Extensions.DependencyInjection;
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
        return services;
    }

    public static Task EnsureMongoIndexesAsync(this IServiceProvider services) =>
        services.GetRequiredService<MongoContext>().EnsureIndexesAsync();
}
