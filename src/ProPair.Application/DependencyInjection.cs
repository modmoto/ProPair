using Microsoft.Extensions.DependencyInjection;
using ProPair.Application.Ports;
using ProPair.Application.UseCases;
using ProPair.Domain.Pairing.Advice;

namespace ProPair.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();

        // The recommendation algorithm. Swap here: another IPairingAdvisor, or the step solver
        // (PessimisticSolver = maximin, EquilibriumSolver = Nash, the default).
        services.AddSingleton<IPairingAdvisor>(new GameTreeAdvisor(new EquilibriumSolver()));
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
