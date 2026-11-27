namespace ProPair.Adapters.NewRecruit.Fake;

/// <summary>
/// Demo army lists in New Recruit text format (army name, "points - entry" lines).
/// Sylvan Elves lists are taken from the public ETC 2026 list thread; the others are realistic demo lists.
/// </summary>
internal static class FakeLists
{
    public static readonly Dictionary<string, string[]> ByArmy = new()
    {
        ["Beast Herds"] =
        [
            """
            440 - Beast Lord, General, Razortusk Chariot, Heavy Armour, Shield, Paired Weapons
            380 - Soothsayer, Wizard Master, Druidism, Dispel Scroll
            260 - Beast Chieftain, Battle Standard Bearer, Heavy Armour, Wild Totem
            420 - 30x Wildhorn Herd, Spear, Standard Bearer, Musician, Champion
            305 - 20x Mongrel Herd, Standard Bearer
            150 - 10x Mongrel Raiders, Throwing Weapons
            150 - 10x Mongrel Raiders, Throwing Weapons
            390 - 4x Minotaurs, Great Weapon, Standard Bearer
            325 - Gortach
            270 - 2x Raiding Chariot
            300 - Gortach
            325 - Razortusk Herd, 6x Razortusks
            """,
            """
            495 - Beast Lord, General, Shield, Heavy Armour, Wild Trophy
            330 - Soothsayer, Wizard Adept, Shamanism, Rod of Battle
            250 - Beast Chieftain, Battle Standard Bearer
            455 - 35x Wildhorn Herd, Standard Bearer, Musician
            150 - 10x Mongrel Raiders
            150 - 10x Mongrel Raiders
            540 - 6x Minotaurs, Paired Weapons, Standard Bearer, Musician
            380 - 8x Centaurs, Standard Bearer
            440 - Cyclops
            310 - Briar Beast
            """,
        ],
        ["Daemon Legions"] =
        [
            """
            700 - Omen of Savagery, General, Wizard Master, Evocation
            390 - Sovereign, Battle Standard Bearer, Hereditary Daemon
            360 - 25x Lemures, Standard Bearer, Musician
            300 - 20x Imps, Standard Bearer
            280 - 5x Hellhounds
            280 - 5x Hellhounds
            190 - 5x Furies
            190 - 5x Furies
            520 - 3x Brazen Beasts
            480 - Kuulima's Deceit
            """,
            """
            560 - Lesser Dominion, General, Wizard Adept, Divination, Hereditary Daemon
            450 - Greater Dominion, Battle Standard Bearer, Wings
            480 - 30x Lemures, Standard Bearer, Musician
            330 - 22x Imps, Standard Bearer
            190 - 5x Furies
            420 - 4x Sentinels of Nukuja
            300 - 6x Hellhounds
            330 - Threshold Guardian
            420 - Kuulima's Deceit
            """,
        ],
        ["Dread Elves"] =
        [
            """
            490 - Dread Prince, General, Dark Steed, Heavy Armour, Shield, Lance
            420 - Oracle, Wizard Master, Occultism, Dispel Scroll
            250 - Dread Noble, Battle Standard Bearer, Heavy Armour
            380 - 25x Dread Legion, Spear, Shield, Standard Bearer, Musician
            220 - 10x Dark Raiders, Repeater Crossbow
            220 - 10x Dark Raiders, Repeater Crossbow
            360 - 6x Dread Knights, Standard Bearer
            330 - 15x Corsairs, Paired Weapons, Standard Bearer
            280 - 2x Raven Chariots
            320 - Medusa
            """,
            """
            560 - Dread Prince, General, Dark Wyvern, Heavy Armour
            375 - Oracle, Wizard Adept, Cosmology, Rod of Battle
            420 - 30x Dread Legion, Spear, Shield, Standard Bearer, Musician
            220 - 10x Dark Raiders, Repeater Crossbow
            240 - 5x Scouts
            420 - 20x Temple Guard, Standard Bearer, Musician
            360 - 6x Dread Knights, Standard Bearer
            280 - 2x Reaper Bolt Throwers
            300 - Hydra
            """,
        ],
        ["Dwarven Holds"] =
        [
            """
            440 - Dwarf King, General, Shield Bearers, Runic Armour
            400 - Runic Smith, Wizard Master, Rune Lore, Anvil of Power
            270 - Thane, Battle Standard Bearer
            500 - 30x Clan Warriors, Shield, Standard Bearer, Musician
            380 - 20x Clan Marksmen, Crossbow, Standard Bearer
            430 - 20x Greybeards, Great Weapon, Standard Bearer, Musician
            250 - 10x Deep Watch, Scouts
            290 - 2x Steam Copters
            300 - Cannon
            250 - Flame Cannon
            """,
            """
            460 - Dwarf King, General, Runic Shield
            310 - Runic Smith, Wizard Adept
            280 - Thane, Battle Standard Bearer, Runic Banner
            560 - 30x Hold Guardians, Standard Bearer, Musician
            400 - 25x Clan Warriors, Shield, Standard Bearer
            290 - 15x Clan Marksmen, Handgun
            300 - 2x Steam Copters
            350 - Cannon
            350 - Organ Gun
            300 - Grudge Buster
            """,
        ],
        ["Empire of Sonnstahl"] =
        [
            """
            460 - Marshal, General, Heavy Armour, Shield, Barded Warhorse, Lance
            430 - Wizard, Wizard Master, Alchemy, Dispel Scroll
            300 - Captain, Battle Standard Bearer, Plate Armour, Barded Warhorse
            420 - 35x State Troops, Spear, Standard Bearer, Musician
            165 - 10x State Militia
            165 - 10x State Militia
            540 - 6x Knightly Order, Standard Bearer, Musician
            380 - 25x Imperial Guard, Halberd, Standard Bearer
            350 - Great Cannon
            300 - Volley Gun
            """,
            """
            500 - Marshal, General, Griffon, Heavy Armour, Lance
            380 - Prelate, Wizard Adept, Divination
            250 - Captain, Battle Standard Bearer
            420 - 35x State Troops, Halberd, Standard Bearer, Musician
            200 - 10x Electoral Cavalry, Pistols
            200 - 10x Electoral Cavalry, Pistols
            600 - 8x Knightly Order, Standard Bearer, Musician
            580 - Steam Tank
            350 - Great Cannon
            """,
        ],
        ["Highborn Elves"] =
        [
            """
            480 - Prince, General, Elein Steed, Heavy Armour, Shield, Lance
            450 - Mage, Wizard Master, Pyromancy, Dispel Scroll
            260 - Commander, Battle Standard Bearer, Heavy Armour
            430 - 30x Citizen Spears, Standard Bearer, Musician
            320 - 15x Citizen Archers, Standard Bearer
            340 - 6x Highborn Lancers, Standard Bearer
            330 - 15x Sword Masters, Standard Bearer
            260 - 5x Reaver Knights, Bows
            300 - 2x Sea Guard Reaper
            300 - Giant Eagles
            """,
            """
            430 - Prince, General, Heavy Armour, Queen's Guard
            370 - Mage, Wizard Adept, Cosmology
            260 - Commander, Battle Standard Bearer
            480 - 30x Sea Guard, Bow, Standard Bearer, Musician
            300 - 20x Lion Guard, Standard Bearer
            340 - 6x Highborn Lancers, Standard Bearer
            300 - 15x Queen's Guard, Standard Bearer
            490 - Flame Wyrm
            260 - 5x Reaver Knights, Bows
            260 - Sky Sloop
            """,
        ],
        ["Infernal Dwarves"] =
        [
            """
            470 - Overlord, General, Great Taurus, Heavy Armour
            420 - Prophet, Wizard Master, Evocation, Dispel Scroll
            280 - Vizier, Battle Standard Bearer
            450 - 30x Infernal Warriors, Shield, Standard Bearer, Musician
            380 - 20x Citadel Guard, Standard Bearer
            180 - 15x Slave Archers
            320 - 3x Taurukh Anointed
            240 - 10x Hobgoblin Wolf Raiders
            580 - Kadim Titan
            300 - Hellfire Rocket Battery
            """,
            """
            500 - Hierophant, General, Wizard Master, Occultism
            280 - Vizier, Battle Standard Bearer, Shield
            480 - 30x Infernal Warriors, Great Weapon, Standard Bearer, Musician
            400 - 20x Disciples of Lugar, Standard Bearer
            180 - 15x Slave Archers
            180 - 15x Slave Archers
            450 - 3x Kadim Incarnates
            500 - Vulcanic Engine
            300 - Infernal Artillery
            """,
        ],
        ["Kingdom of Equitaine"] =
        [
            """
            480 - Duke, General, Barded Warhorse, Plate Armour, Shield, Lance, Questing Oath
            430 - Damsel, Wizard Master, Druidism, Dispel Scroll
            260 - Paladin, Battle Standard Bearer, Barded Warhorse
            560 - 8x Knights of the Realm, Standard Bearer, Musician
            450 - 6x Knights Aspirant, Standard Bearer
            200 - 20x Peasant Levy, Bows
            200 - 20x Peasant Levy, Bows
            480 - 5x Grail Knights, Standard Bearer
            360 - 3x Pegasus Knights
            """,
            """
            560 - Duke, General, Hippogriff, Plate Armour, Shield, Lance
            370 - Damsel, Wizard Adept, Divination
            260 - Paladin, Battle Standard Bearer, Barded Warhorse
            540 - 8x Knights of the Realm, Standard Bearer
            300 - 5x Knights Forlorn
            300 - 5x Knights Forlorn
            200 - 20x Peasant Levy, Bows
            480 - 6x Knights of the Grail, Standard Bearer
            420 - 3x Hippogriff Knights
            """,
        ],
        ["Ogre Khans"] =
        [
            """
            520 - Great Khan, General, Heavy Armour, Paired Weapons
            440 - Shaman, Wizard Master, Shamanism, Dispel Scroll
            260 - Chief, Battle Standard Bearer
            600 - 8x Ogres, Ironfist, Standard Bearer, Musician
            510 - 6x Bruisers, Standard Bearer
            150 - 20x Gnoblars
            150 - 20x Gnoblars
            420 - 3x Rock Aurochs
            380 - 2x Yetis
            300 - Thunder Fist
            """,
            """
            480 - Khan, General, Sabretooth Tiger
            410 - Butcher, Wizard Master, Thaumaturgy
            300 - Chief, Battle Standard Bearer, Heavy Armour
            540 - 7x Ogres, Paired Weapons, Standard Bearer
            420 - 4x Mercenary Veterans
            150 - 20x Gnoblars
            380 - 2x Rock Aurochs
            330 - 4x Sabretooth Tigers
            420 - Mammoth
            """,
        ],
        ["Orcs and Goblins"] =
        [
            """
            440 - Orc Warlord, General, Iron Orc, Heavy Armour, Shield
            430 - Orc Shaman, Wizard Master, Shamanism, Dispel Scroll
            240 - Orc Chief, Battle Standard Bearer
            450 - 30x Iron Orcs, Shield, Standard Bearer, Musician
            300 - 40x Goblins, Bows, Standard Bearer
            180 - 10x Goblin Wolf Riders, Bows
            180 - 10x Goblin Wolf Riders, Bows
            420 - 6x Trolls, Standard Bearer
            300 - 2x Boar Chariots
            420 - Giant
            """,
            """
            480 - Orc Warlord, General, War Boar, Heavy Armour
            380 - Goblin Shaman, Wizard Master, Witchcraft
            240 - Orc Chief, Battle Standard Bearer
            420 - 30x Common Orcs, Paired Weapons, Standard Bearer, Musician
            460 - 10x Feral Orc Boar Riders, Standard Bearer
            300 - 40x Goblins, Bows
            380 - 3x Gnasher Herds
            380 - 6x Trolls
            300 - Scrap Wagon
            """,
        ],
        ["Saurian Ancients"] =
        [
            """
            560 - Saurian Veteran, General, Raptor, Heavy Armour, Shield
            460 - Skink Priest, Wizard Master, Divination, Dispel Scroll
            260 - Saurian Veteran, Battle Standard Bearer
            480 - 30x Saurian Warriors, Spear, Standard Bearer, Musician
            160 - 10x Skink Braves, Javelins
            160 - 10x Skink Braves, Javelins
            380 - 5x Raptor Riders, Standard Bearer
            420 - 20x Temple Guard, Standard Bearer
            560 - Ancient Taurosaur
            """,
            """
            620 - Caiman Ancient, General, Wizard Master, Cosmology
            280 - Saurian Veteran, Battle Standard Bearer
            480 - 30x Saurian Warriors, Spear, Standard Bearer
            160 - 10x Skink Braves
            160 - 10x Skink Hunters
            380 - 5x Raptor Riders
            440 - 3x Ancient Caimans
            480 - Taurosaur
            400 - 2x Terradon Riders
            """,
        ],
        ["Sylvan Elves"] =
        [
            """
            480 - Avatar of Nature
            455 - Druid, General, Light Troops, Wizard Master, Cosmology (Truth of Time, Weal and Woe, Ice and Fire, Cosmic Scales, Thunder and Lightning), Sylvan Bow, Spell Scroll (Awaken the Beast)
            310 - Thicket Shepherd, Battle Standard Bearer, Scarred Bark
            215 - Dryad Ancient, Captain and Attached, Wizard Adept, Druidism (Fountain of Youth, Veil of Mist, Stone Skin)
            403 - 19x Sylvan Archers, Standard Bearer
            372 - 23x Dryads, Musician, Standard Bearer
            225 - 20x Forest Guard, Standard Bearer
            395 - 5x Thicket Beasts, Standard Bearer
            245 - 5x Wild Huntsmen, Lance
            200 - 10x Forest Rangers, Standard Bearer
            200 - 10x Forest Rangers, Standard Bearer
            264 - 8x Sylvan Sentinels
            236 - 7x Sylvan Sentinels
            """,
            """
            315 - Forest Prince, Light Troops, Sylvan Bow, Great Weapon and Elven Finesse (Supernatural Dexterity), Pathfinder
            310 - Thicket Shepherd, Battle Standard Bearer, Scarred Bark
            245 - Chieftain, General, Light Troops, Sylvan Bow, Great Weapon and Elven Finesse (Eldritch Inscriptions), Pathfinder
            235 - Chieftain, Captain and Attached, Light Troops, Sylvan Bow, Spear (Hunter's Honour), Forest Guardian
            387 - 32x Forest Guard, Shield, Standard Bearer, Musician
            379 - 17x Sylvan Archers, Standard Bearer, Musician
            180 - 5x Heath Riders, Heath Lancers, Standard Bearer
            180 - 5x Heath Riders, Heath Lancers, Standard Bearer
            395 - 5x Thicket Beasts, Standard Bearer
            350 - 7x Wild Huntsmen, Shield, Lance, Standard Bearer
            350 - 7x Wild Huntsmen, Shield, Lance, Standard Bearer
            350 - 7x Wild Huntsmen, Shield, Lance, Standard Bearer
            320 - 10x Sylvan Sentinels
            """,
        ],
        ["Undying Dynasties"] =
        [
            """
            500 - Pharaoh, General, Chariot, Heavy Armour, Shield
            450 - Death Cult Hierarch, Wizard Master, Pyromancy, Dispel Scroll
            260 - Royal Herald, Battle Standard Bearer
            400 - 30x Skeleton Infantry, Spear, Shield, Standard Bearer
            260 - 20x Skeleton Archers
            480 - 6x Tomb Cataphracts, Standard Bearer
            330 - 3x Sand Stalkers
            420 - 20x Shabtis, Standard Bearer
            300 - Casket of Ages
            """,
            """
            560 - Pharaoh, General, Sphinx
            420 - Death Cult Hierarch, Wizard Master, Divination
            260 - Royal Herald, Battle Standard Bearer, Heavy Armour
            420 - 30x Skeleton Infantry, Standard Bearer, Musician
            260 - 20x Skeleton Archers
            260 - 2x Skeleton Chariots
            330 - 3x Sand Stalkers
            480 - 3x Necropolis Titans
            300 - Colossus
            """,
        ],
        ["Vampire Covenant"] =
        [
            """
            560 - Vampire Count, General, Heavy Armour, Shield, Nightmare
            440 - Necromancer, Wizard Master, Occultism, Dispel Scroll
            300 - Barrow King, Battle Standard Bearer
            300 - 30x Skeletons, Spear, Standard Bearer
            240 - 30x Zombies
            520 - 6x Vampire Knights, Standard Bearer
            300 - 6x Barrow Knights
            200 - 5x Dire Wolves
            200 - 5x Dire Wolves
            380 - 3x Varkolak
            300 - Phantom Hosts
            """,
            """
            600 - Vampire Count, General, Dragon
            380 - Necromancer, Wizard Adept, Evocation
            260 - Vampire Courtier, Battle Standard Bearer
            440 - 40x Skeletons, Standard Bearer, Musician
            200 - 5x Dire Wolves
            200 - 5x Bat Swarms
            480 - 8x Barrow Knights, Standard Bearer
            420 - Varkolak
            420 - 2x Great Bats
            """,
        ],
        ["Vermin Swarm"] =
        [
            """
            480 - Sovereign, General, Heavy Armour, Shield
            460 - Hierarch, Wizard Master, Thaumaturgy, Dispel Scroll
            260 - Chief, Battle Standard Bearer
            420 - 40x Vermin Guard, Standard Bearer, Musician
            320 - 40x Vermin Slaves
            300 - 30x Plague Brotherhood, Standard Bearer
            280 - 5x Jezails
            380 - 4x Rat Ogres
            350 - Lightning Cannon
            350 - Plague Catapult
            """,
            """
            500 - Hierarch, General, Wizard Master, Occultism
            260 - Chief, Battle Standard Bearer
            440 - 40x Vermin Guard, Standard Bearer, Musician
            320 - 40x Vermin Slaves
            300 - 30x Plague Brotherhood
            280 - 5x Jezails
            280 - 5x Jezails
            460 - 6x Rat Ogres, Packmasters
            350 - Lightning Cannon
            450 - Vermin Daemon
            """,
        ],
        ["Warriors of the Dark Gods"] =
        [
            """
            580 - Chosen Lord, General, Dark Steed, Plate Armour, Shield, Lance
            460 - Sorcerer, Wizard Master, Occultism, Dispel Scroll
            280 - Barbarian Chief, Battle Standard Bearer
            520 - 15x Warriors, Halberd, Shield, Standard Bearer, Musician
            280 - 5x Barbarian Horsemen
            280 - 5x Barbarian Horsemen
            160 - 5x Warhounds
            160 - 5x Warhounds
            600 - 5x Chosen Knights, Standard Bearer
            400 - Forsaken One
            """,
            """
            640 - Chosen Lord, General, Feldrak, Plate Armour
            420 - Sorcerer, Wizard Adept, Pyromancy
            280 - Barbarian Chief, Battle Standard Bearer
            540 - 15x Chosen Warriors, Great Weapon, Standard Bearer
            300 - 20x Fallen
            160 - 5x Warhounds
            160 - 5x Warhounds
            560 - 5x Chosen Knights
            320 - 3x Fallen
            400 - Forsaken One
            """,
        ],
    };

    /// <summary>Returns a list in New Recruit export format, including army name header and total.</summary>
    public static string For(string army, int variant)
    {
        var lists = ByArmy[army];
        var body = lists[variant % lists.Length].Trim();
        var total = body.Split('\n').Sum(l => int.TryParse(l.Split(" - ")[0].Trim(), out var pts) ? pts : 0);
        return $"{army}\n{body}\n{total}";
    }
}
