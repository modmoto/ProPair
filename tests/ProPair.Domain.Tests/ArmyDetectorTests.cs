using ProPair.Domain.GameSystems;

namespace ProPair.Domain.Tests;

public class ArmyDetectorTests
{
    [Theory]
    [InlineData("Ogre Khans\n500 - Great Khan", "ogre-khans")]
    [InlineData("Max Muster (Maxi) - Vampire Covenant\n\n450 - Vampire Count", "vampire-covenant")]
    [InlineData("Empire of Sonnstahl - Hans\n400 - Marshal", "empire-of-sonnstahl")]
    [InlineData("Pacte Vampirique\n450 - Comte", "vampire-covenant")]       // French
    [InlineData("== Turnier ==\nSpieler: X\nHighborn Elves\n300 - Prince", "highborn-elves")] // not in the first line
    [InlineData("Dread Fleet\n300 - Captain", "dread-elves")]
    public void Recognises_the_army(string list, string expected) => Assert.Equal(expected, ArmyDetector.Detect(list));

    [Fact]
    public void Falls_back_to_typical_units() =>
        Assert.Equal("ogre-khans", ArmyDetector.Detect("My list\n500 - Great Khan\n300 - Bruisers\n200 - Sabretooth Tigers"));

    [Fact]
    public void Unknown_when_nothing_matches() => Assert.Equal(ArmyDetector.Unknown, ArmyDetector.Detect("hello"));
}
