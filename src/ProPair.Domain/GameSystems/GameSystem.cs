namespace ProPair.Domain.GameSystems;

/// <summary>Reference data: a game system and its armies.</summary>
public sealed class GameSystem(string id, string name, IReadOnlyList<Army> armies)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public IReadOnlyList<Army> Armies { get; } = armies;

    /// <summary>Army that could not be recognised in a list text.</summary>
    public static readonly Army UnknownArmy = new(ArmyDetector.Unknown, "Unbekannte Armee", "?");

    public Army? FindArmy(string idOrName) => idOrName == ArmyDetector.Unknown ? UnknownArmy :
        Armies.FirstOrDefault(a =>
            string.Equals(a.Id, idOrName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a.Name, idOrName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a.Short, idOrName, StringComparison.OrdinalIgnoreCase));

    public static readonly GameSystem NinthAge = new("t9a", "The 9th Age",
    [
        new("beast-herds", "Beast Herds", "BH"),
        new("daemon-legions", "Daemon Legions", "DL"),
        new("dread-elves", "Dread Elves", "DE"),
        new("dwarven-holds", "Dwarven Holds", "DH"),
        new("empire-of-sonnstahl", "Empire of Sonnstahl", "EoS"),
        new("highborn-elves", "Highborn Elves", "HbE"),
        new("infernal-dwarves", "Infernal Dwarves", "ID"),
        new("kingdom-of-equitaine", "Kingdom of Equitaine", "KoE"),
        new("ogre-khans", "Ogre Khans", "OK"),
        new("orcs-and-goblins", "Orcs and Goblins", "O&G"),
        new("saurian-ancients", "Saurian Ancients", "SA"),
        new("sylvan-elves", "Sylvan Elves", "SE"),
        new("undying-dynasties", "Undying Dynasties", "UD"),
        new("vampire-covenant", "Vampire Covenant", "VC"),
        new("vermin-swarm", "Vermin Swarm", "VS"),
        new("warriors-of-the-dark-gods", "Warriors of the Dark Gods", "WDG"),
    ]);
}
