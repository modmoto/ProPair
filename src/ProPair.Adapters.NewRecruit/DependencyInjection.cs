using Microsoft.Extensions.DependencyInjection;
using ProPair.Adapters.NewRecruit.Api;
using ProPair.Adapters.NewRecruit.Fake;
using ProPair.Application.Ports;

namespace ProPair.Adapters.NewRecruit;

public static class DependencyInjection
{
    /// <summary>Demo tournaments only (the API process: offers them for trying the app).</summary>
    public static IServiceCollection AddDemoTournaments(this IServiceCollection services)
    {
        services.AddSingleton<ITournamentSource, FakeTournamentSource>();
        return services;
    }

    /// <summary>The real New Recruit API (the sync process).</summary>
    public static IServiceCollection AddNewRecruit(this IServiceCollection services, NewRecruitOptions options)
    {
        if (string.IsNullOrEmpty(options.Login) || string.IsNullOrEmpty(options.Password))
            throw new InvalidOperationException("NewRecruit:Login / NewRecruit:Password missing (user secrets or env vars).");
        services.AddSingleton(options);
        services.AddHttpClient<ITournamentSource, NewRecruitClient>(c => c.Timeout = TimeSpan.FromSeconds(60));
        return services;
    }
}
