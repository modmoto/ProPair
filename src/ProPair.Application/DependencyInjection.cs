using Microsoft.Extensions.DependencyInjection;
using ProPair.Application.Ports;
using ProPair.Application.UseCases;

namespace ProPair.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();

        // The recommendation algorithm per mode (Optimal, AvoidMinus, Safe). Swap the provider – or change
        // AdviceModes.Create – to plug in another IPairingAdvisor or step solver.
        services.AddSingleton<IPairingAdvisorProvider, DefaultPairingAdvisorProvider>();
        services.AddScoped<SearchSourceTournaments>();
        services.AddScoped<ImportTournament>();
        services.AddScoped<StartPreparation>();
        services.AddScoped<GetPreparation>();
        services.AddScoped<GetMatrix>();
        services.AddScoped<SetRating>();
        services.AddScoped<FillDemoRatings>();
        services.AddScoped<SetCellNote>();
        services.AddScoped<GetCellHistory>();
        services.AddScoped<SetPlayerNote>();
        services.AddScoped<SetListMark>();
        services.AddScoped<GetPairing>();
        services.AddScoped<PlayPairingMove>();
        services.AddScoped<UndoPairingMove>();
        services.AddScoped<RestartPairing>();
        return services;
    }
}
