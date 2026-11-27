using System.Globalization;
using System.Text;

namespace ProPair.Domain.GameSystems;

/// <summary>
/// Finds the army of a list text. New Recruit lists vary: "Ogre Khans", "Max (Nick) - Ogre Khans",
/// "Ogre Khans - Max", footer lines, translated army names. Looks at the first lines of the list
/// and matches known names in several languages (accents and case ignored, longest match wins).
/// </summary>
public static class ArmyDetector
{
    public const string Unknown = "unknown";
    private const int LinesToCheck = 6;

    private static readonly (string ArmyId, string Alias)[] Aliases = BuildAliases();

    public static string Detect(string listText)
    {
        var lines = listText.Replace("\r", "").Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith("Created with", StringComparison.OrdinalIgnoreCase))
            .Take(LinesToCheck);
        foreach (var line in lines)
        {
            var normalized = Normalize(line);
            var hit = Aliases.Where(a => Contains(normalized, a.Alias)).OrderByDescending(a => a.Alias.Length).FirstOrDefault();
            if (hit.ArmyId is not null) return hit.ArmyId;
        }

        // no army name in the header: recognise the army by its typical units (the one with most hits wins)
        var all = Normalize(listText);
        var byUnits = UnitHints
            .Select(h => (h.ArmyId, Hits: h.Units.Count(u => Contains(all, u))))
            .Where(x => x.Hits > 0)
            .OrderByDescending(x => x.Hits)
            .FirstOrDefault();
        return byUnits.ArmyId ?? Unknown;
    }

    /// <summary>Units only one army has – fallback when a list has no army name.</summary>
    private static readonly (string ArmyId, string[] Units)[] UnitHints = new (string, string[])[]
    {
        ("beast-herds", ["Wildhorn Herd", "Mongrel Herd", "Minotaurs", "Gortach", "Razortusk", "Centaur Chieftain", "Soothsayer"]),
        ("daemon-legions", ["Lemures", "Hellhounds", "Kuulima", "Sentinels of Nukuja", "Omen of Savagery", "Brazen Beasts"]),
        ("dread-elves", ["Silexian", "Dread Knights", "Temple Militants", "Dread Acolytes", "Shadow Riders", "Tethaen"]),
        ("dwarven-holds", ["Runic Smith", "Greybeards", "Hold Guardians", "Clan Warriors", "Clan Marksmen", "Deep Watch"]),
        ("empire-of-sonnstahl", ["State Troops", "State Militia", "Knightly Order", "Electoral Cavalry", "Imperial Guard", "Steam Tank"]),
        ("highborn-elves", ["Citizen Spears", "Citizen Archers", "Sea Guard", "Lion Guard", "Sword Masters", "Queen's Guard", "Highborn Lancers"]),
        ("infernal-dwarves", ["Infernal Warriors", "Citadel Guard", "Disciples of Lugar", "Kadim", "Taurukh", "Vizier", "Goblin Witch", "Goblin Demagogue", "Darrmu", "Hobgoblin"]),
        ("kingdom-of-equitaine", ["Knights of the Realm", "Knights Aspirant", "Knights Forlorn", "Peasant Levy", "Damsel", "Grail"]),
        ("ogre-khans", ["Bruisers", "Gnoblars", "Sabretooth", "Rock Aurochs", "Mercenary Veterans", "Great Khan"]),
        ("orcs-and-goblins", ["Iron Orcs", "Common Orcs", "Feral Orcs", "Gnasher", "Grotlings", "Orc Warlord", "Great Green Idol"]),
        ("saurian-ancients", ["Saurian Warriors", "Skink", "Raptor Riders", "Temple Guard", "Caiman", "Taurosaur"]),
        ("sylvan-elves", ["Dryad", "Forest Guard", "Sylvan Archers", "Thicket", "Wild Huntsmen", "Forest Rangers", "Kestrel Knights"]),
        ("undying-dynasties", ["Skeleton Infantry", "Tomb Cataphracts", "Sand Stalkers", "Shabtis", "Death Cult Hierarch", "Casket of Ages"]),
        ("vampire-covenant", ["Vampire Count", "Barrow Knights", "Vampire Knights", "Dire Wolves", "Phantom Hosts", "Varkolak"]),
        ("vermin-swarm", ["Vermin Guard", "Vermin Slaves", "Plague Brotherhood", "Rat Ogres", "Jezails", "Lightning Cannon", "Swarm Priest", "Pestilent Pulpit", "Sacred Platform"]),
        ("warriors-of-the-dark-gods", ["Chosen Warriors", "Chosen Knights", "Barbarian Horsemen", "Warhounds", "Forsaken One", "Feldrak"]),
    }.Select(h => (h.Item1, h.Item2.Select(Normalize).ToArray())).ToArray();

    private static bool Contains(string line, string alias)
    {
        var index = line.IndexOf(alias, StringComparison.Ordinal);
        while (index >= 0)
        {
            var before = index == 0 || !char.IsLetter(line[index - 1]);
            var end = index + alias.Length;
            var after = end == line.Length || !char.IsLetter(line[end]);
            if (before && after) return true; // whole words only ("DE" must not match inside "Defender")
            index = line.IndexOf(alias, index + 1, StringComparison.Ordinal);
        }
        return false;
    }

    private static string Normalize(string text)
    {
        var decomposed = text.ToLowerInvariant().Replace('’', '\'').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString();
    }

    private static (string, string)[] BuildAliases()
    {
        // English names come from the game system; translations (FR/DE/ES/IT/PL) seen on New Recruit or common.
        var extra = new Dictionary<string, string[]>
        {
            ["beast-herds"] = ["Hordes Bestiales", "Tierhorden", "Hordas Bestiales", "Orde Bestiali", "Zwierzoludzie"],
            ["daemon-legions"] = ["Demon Legions", "Légions Démoniaques", "Legions Demoniaques", "Dämonenlegionen", "Legiones Demoníacas", "Legioni Demoniache"],
            ["dread-elves"] = ["Elfes Noirs", "Elfes de l'Effroi", "Schreckenselfen", "Elfos Oscuros", "Elfi Oscuri", "Mroczne Elfy", "Dread Fleet"],
            ["dwarven-holds"] = ["Forteresses Naines", "Zwergenreiche", "Fortalezas Enanas", "Roccaforti Naniche", "Krasnoludy"],
            ["empire-of-sonnstahl"] = ["Empire de Sonnstahl", "Imperium von Sonnstahl", "Imperio de Sonnstahl", "Impero di Sonnstahl", "Sonnstahl"],
            ["highborn-elves"] = ["Hautes Lignées Elfiques", "Hauts Elfes", "Hochgeborene Elfen", "Altos Elfos", "Elfi Alti", "Wysokie Elfy"],
            ["infernal-dwarves"] = ["Nains Infernaux", "Höllenzwerge", "Enanos Infernales", "Nani Infernali"],
            ["kingdom-of-equitaine"] = ["Royaume d'Équitaine", "Royaume d'Equitaine", "Königreich Equitaine", "Reino de Equitania", "Regno di Equitania", "Equitaine"],
            ["ogre-khans"] = ["Khans Ogres", "Ogerkhane", "Khanes Ogros", "Khan Ogri"],
            ["orcs-and-goblins"] = ["Orques et Gobelins", "Orks und Goblins", "Orcos y Goblins", "Orchi e Goblin", "O&G"],
            ["saurian-ancients"] = ["Anciens Sauriens", "Saurische Urahnen", "Antiguos Saurios", "Antichi Sauri"],
            ["sylvan-elves"] = ["Elfes Sylvains", "Waldelfen", "Elfos Silvanos", "Elfi Silvani", "Leśne Elfy"],
            ["undying-dynasties"] = ["Dynasties Immortelles", "Unsterbliche Dynastien", "Dinastías Inmortales", "Dinastie Immortali"],
            ["vampire-covenant"] = ["Covenant Vampire", "Pacte Vampirique", "Vampirbund", "Pacto Vampírico", "Patto Vampirico"],
            ["vermin-swarm"] = ["Marée de Vermine", "Maree de Vermine", "Ungezieferschwarm", "Enjambre de Alimañas", "Sciame di Parassiti"],
            ["warriors-of-the-dark-gods"] = ["Guerriers des Dieux Sombres", "Krieger der Dunklen Götter", "Guerreros de los Dioses Oscuros", "Guerrieri degli Dei Oscuri"],
        };

        return GameSystem.NinthAge.Armies
            .SelectMany(a => new[] { a.Name }.Concat(extra.GetValueOrDefault(a.Id, [])).Select(alias => (a.Id, Normalize(alias))))
            .Distinct()
            .ToArray();
    }
}
