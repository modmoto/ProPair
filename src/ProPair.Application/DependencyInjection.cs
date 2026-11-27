using Microsoft.Extensions.DependencyInjection;
using ProPair.Application.Ports;
using ProPair.Application.UseCases;

namespace ProPair.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<SearchSourceTournaments>();
        services.AddScoped<ImportTournament>();
        services.AddScoped<StartPreparation>();
        services.AddScoped<GetPreparation>();
        services.AddScoped<GetMatrix>();
        services.AddScoped<SetRating>();
        return services;
    }
}
