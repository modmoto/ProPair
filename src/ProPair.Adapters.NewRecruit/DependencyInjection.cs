using Microsoft.Extensions.DependencyInjection;
using ProPair.Adapters.NewRecruit.Fake;
using ProPair.Application.Ports;

namespace ProPair.Adapters.NewRecruit;

public static class DependencyInjection
{
    /// <summary>Registers the tournament source. Fake until New Recruit grants API access.</summary>
    public static IServiceCollection AddNewRecruit(this IServiceCollection services)
    {
        services.AddSingleton<ITournamentSource, FakeTournamentSource>();
        return services;
    }
}
