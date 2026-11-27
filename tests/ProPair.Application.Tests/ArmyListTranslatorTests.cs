using ProPair.Adapters.NewRecruit.Acl;
using ProPair.Domain.Tournaments;

namespace ProPair.Application.Tests;

public class ArmyListTranslatorTests
{
    private const string Units = "480 - Warlord, General\n415 - 24 Militants, Standard Bearer\n195 - 10 Corsairs";

    private static string Clean(params string[] raw) => ArmyListTranslator.Translate("p", raw).ListText;

    [Theory]
    [InlineData("Max (Maxi) - Dread Elves\n" + Units + "\n4000\nCreated with newrecruit.eu v1.2")]
    [InlineData("Dread Elves\r\n" + Units + "\r\n4000\r\n")]
    [InlineData("Text Export\n Export Options\nDread Elves\n" + Units + "\n3995 pts")]
    [InlineData("jc - Dread Elves\nCharacters (Max. 40%) : 555 / 1600\nMounts : 0\n\n" + Units)]
    public void Headers_totals_and_footer_are_removed(string raw)
    {
        var player = ArmyListTranslator.Translate("p", [raw]);

        Assert.Equal(Units, player.ListText);
        Assert.Equal("dread-elves", player.ArmyId); // read from the header before it was cut
    }

    [Fact]
    public void A_list_handed_in_twice_counts_once()
    {
        var one = "Dread Elves\n" + Units + "\n4000";

        Assert.Equal(Units, Clean(one + "\n\n" + one));
        Assert.Equal(Units, Clean(one, one));
    }

    [Fact]
    public void Two_different_lists_are_both_kept()
    {
        var other = "300 - Beast\n200 - Altar\n100 - Harpies";

        Assert.Equal(Units + "\n\n" + other, Clean("Dread Elves\n" + Units + "\n4000", "Dread Elves\n" + other + "\n3995"));
    }

    [Fact]
    public void Extras_after_the_total_are_kept() =>
        Assert.Equal(Units + "\n\n+ Mercenary 300", Clean(Units + "\n4000\n+ Mercenary 300\n4000"));

    [Theory]
    [InlineData("kadrowicz\n0")]
    [InlineData("ETC player\n0")]
    [InlineData("  ")]
    public void Placeholders_are_no_list(string raw)
    {
        var player = ArmyListTranslator.Translate("p", [raw]);

        Assert.Equal("", player.ListText);
        Assert.Equal("unknown", player.ArmyId);
    }

    [Fact]
    public void Unknown_formats_only_lose_total_and_footer()
    {
        var raw = "Joonatan - Ogre Khans\nCharacters:\n- Khan 275 p.\no Iron Fist\n- Shaman 320 p.\n- Shaman 540 p.";

        Assert.Equal(raw, Clean(raw + "\n4000\nCreated with newrecruit.eu v1"));
    }

    [Theory]
    [InlineData("Max (Maxi) - Dread Elves\n" + Units + "\n4000\nCreated with newrecruit.eu v1.2")]
    [InlineData("Dread Elves\n575 Beastlord\n" + Units + "\n4000")]
    public void Cleaning_does_not_count_as_a_new_list(string raw) => Assert.True(ArmyList.Same(raw, Clean(raw)));
}
