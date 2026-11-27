using ProPair.Application.Ports;

namespace ProPair.Adapters.NewRecruit.Fake;

/// <summary>
/// Demo tournaments for trying the app without New Recruit. Teams and players are fictional;
/// army spread follows ETC 2026.
/// </summary>
public sealed class FakeTournamentSource : ITournamentSource
{
    public Task<IReadOnlyList<SourceTournamentSummary>> SearchAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SourceTournamentSummary>>(Tournaments
            .Select(t => new SourceTournamentSummary(t.ExternalId, t.Name, t.StartDate, t.EndDate, SourceStatus.Open))
            .ToList());

    public Task<SourceTournament?> GetAsync(string externalId, CancellationToken ct = default) =>
        Task.FromResult(Tournaments.FirstOrDefault(t => t.ExternalId == externalId));

    private static readonly Dictionary<string, string> ArmyByShort = new()
    {
        ["BH"] = "Beast Herds", ["DL"] = "Daemon Legions", ["DE"] = "Dread Elves", ["DH"] = "Dwarven Holds",
        ["EoS"] = "Empire of Sonnstahl", ["HbE"] = "Highborn Elves", ["ID"] = "Infernal Dwarves",
        ["KoE"] = "Kingdom of Equitaine", ["OK"] = "Ogre Khans", ["O&G"] = "Orcs and Goblins",
        ["SA"] = "Saurian Ancients", ["SE"] = "Sylvan Elves", ["UD"] = "Undying Dynasties",
        ["VC"] = "Vampire Covenant", ["VS"] = "Vermin Swarm", ["WDG"] = "Warriors of the Dark Gods",
    };

    private static readonly string[] FirstNames =
    [
        "Lukas", "Jonas", "Felix", "Max", "Paul", "Leon", "Elias", "Noah", "Tim", "Jan", "Marco", "Sven",
        "Tobias", "Florian", "Matteo", "David", "Simon", "Nico", "Fabian", "Moritz", "Julian", "Erik", "Luca",
        "Ben", "Kai", "Ole", "Rafael", "Hugo", "Oskar", "Theo", "Adrian", "Lars", "Pablo", "Tomasz", "Piotr",
        "Giulio", "Andrea", "Javier", "Oliver", "Harry",
    ];

    private static readonly string[] LastInitials = ["A", "B", "D", "F", "G", "H", "K", "L", "M", "N", "P", "R", "S", "T", "W", "Z"];

    // declared after the lookup tables: static fields initialize in textual order
    private static readonly IReadOnlyList<SourceTournament> Tournaments =
    [
        Build("demo-etc-2026", "ETC 2026 (Demo)", new DateOnly(2026, 7, 23), 8,
        [
            ("Germany", ["EoS", "OK", "WDG", "VC", "UD", "DE", "HbE", "DH"]),
            ("Austria", ["OK", "EoS", "VC", "DL", "ID", "KoE", "SE", "WDG"]),
            ("Poland", ["WDG", "OK", "EoS", "UD", "DE", "O&G", "VS", "DL"]),
            ("Italy", ["HbE", "EoS", "OK", "VC", "DH", "BH", "WDG", "KoE"]),
            ("Spain", ["SA", "OK", "UD", "EoS", "ID", "DE", "SE", "VC"]),
            ("England", ["KoE", "WDG", "EoS", "O&G", "HbE", "DL", "VS", "UD"]),
        ]),
        Build("demo-dtm-2026", "Deutsche Team-Meisterschaft 2026 (Demo)", new DateOnly(2026, 10, 10), 5,
        [
            ("Würfelwölfe Würzburg", ["EoS", "VC", "OK", "DE", "SE"]),
            ("Tabletop Tigers Köln", ["WDG", "UD", "HbE", "DH", "BH"]),
            ("Ninth Legion Berlin", ["OK", "DL", "ID", "KoE", "VS"]),
            ("Rheinland Raiders", ["VC", "EoS", "SA", "O&G", "WDG"]),
            ("Bavarian Battle Brothers", ["DE", "UD", "OK", "SE", "DH"]),
            ("Hanse Haudegen", ["HbE", "WDG", "EoS", "DL", "ID"]),
        ]),
        Build("demo-stm-2026", "Swiss Team Masters 2026 (Demo)", new DateOnly(2026, 11, 14), 5,
        [
            ("Alpine Avengers", ["KoE", "OK", "VC", "DH", "BH"]),
            ("Lake Geneva Lancers", ["EoS", "HbE", "WDG", "UD", "VS"]),
            ("Zurich Zealots", ["DE", "ID", "OK", "SE", "DL"]),
            ("Bernese Bastion", ["WDG", "VC", "O&G", "SA", "EoS"]),
        ]),
        Build("demo-trio-2026", "Trio Cup Wien 2026 (Demo)", new DateOnly(2026, 12, 5), 3,
        [
            ("Donau Drachen", ["EoS", "DE", "OK"]),
            ("Prater Pikemen", ["WDG", "VC", "HbE"]),
            ("Schönbrunn Schützen", ["DH", "UD", "KoE"]),
            ("Graz Grimdark", ["OK", "DL", "SE"]),
            ("Linz Legion", ["ID", "VS", "EoS"]),
            ("Salzburg Sentinels", ["BH", "O&G", "WDG"]),
        ]),
    ];

    private static SourceTournament Build(string id, string name, DateOnly start, int size,
        (string Team, string[] Armies)[] teams)
    {
        var seed = id.Length;
        var built = teams.Select((t, ti) =>
        {
            if (t.Armies.Length != size) throw new InvalidOperationException($"{name}/{t.Team}: wrong roster size");
            var players = t.Armies.Select((shortName, pi) =>
            {
                var army = ArmyByShort[shortName];
                var n = seed + ti * 7 + pi * 3;
                var player = $"{FirstNames[n % FirstNames.Length]} {LastInitials[(n * 5 + ti) % LastInitials.Length]}.";
                return new SourcePlayer(player, [FakeLists.For(army, ti + pi)]);
            }).ToList();
            return new SourceTeam(t.Team, players);
        }).ToList();

        return new SourceTournament(id, name, start, start.AddDays(2), IsTeamTournament: true, ListsVisible: true, built);
    }
}
