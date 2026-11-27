using ProPair.Domain.Common;
using ProPair.Domain.Preparations;
using ProPair.Domain.Teams;
using ProPair.Domain.Tournaments;

namespace ProPair.Domain.Tests;

public class ListMarkTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);
    private const string List = "Empire of Sonnstahl\n165 - 10x State Militia\n165 - 10x State Militia\n350 - Great Cannon\n680";

    private static (Preparation, PlayerId) NewPreparation()
    {
        var players = Enumerable.Range(0, 3).Select(i => new Player(PlayerId.New(), $"P{i}", "empire-of-sonnstahl", List)).ToList();
        var t = new Tournament(TournamentId.New(), "x", "Cup", "t9a", new TeamSize(3), null,
            [new Participant(ParticipantId.New(), "A", players)]);
        return (Preparation.Start(Team.Create("A"), t, t.Participants[0].Id), players[0].Id);
    }

    [Fact]
    public void Mark_line_with_colour_and_note_then_remove()
    {
        var (prep, player) = NewPreparation();

        prep.MarkListLine(player, List, 3, "350 - Great Cannon", MarkColor.Red, " kann Flammenwerfer ", "Simon", Now);
        var mark = Assert.Single(prep.MarksFor(player, List));
        Assert.Equal((3, MarkColor.Red, "kann Flammenwerfer"), (mark.LineIndex, mark.Color, mark.Note));

        prep.MarkListLine(player, List, 3, "350 - Great Cannon", null, null, "Simon", Now);
        Assert.Empty(prep.MarksFor(player, List));
    }

    [Fact]
    public void Mark_rejects_line_that_does_not_match() =>
        Assert.Throws<DomainException>(() =>
        {
            var (prep, player) = NewPreparation();
            prep.MarkListLine(player, List, 3, "999 - Steam Tank", MarkColor.Red, null, "Simon", Now);
        });

    [Fact]
    public void Duplicate_lines_are_marked_independently()
    {
        var (prep, player) = NewPreparation();

        prep.MarkListLine(player, List, 2, "165 - 10x State Militia", MarkColor.Yellow, null, "Simon", Now);

        Assert.Equal([2], prep.MarksFor(player, List).Select(m => m.LineIndex));
    }

    [Fact]
    public void Marks_follow_their_line_after_list_changes()
    {
        var (prep, player) = NewPreparation();
        prep.MarkListLine(player, List, 3, "350 - Great Cannon", MarkColor.Blue, null, "Simon", Now);

        var reordered = "Empire of Sonnstahl\n350 - Great Cannon\n165 - 10x State Militia\n515";
        Assert.Equal([1], prep.MarksFor(player, reordered).Select(m => m.LineIndex));

        var without = "Empire of Sonnstahl\n165 - 10x State Militia\n165";
        Assert.Empty(prep.MarksFor(player, without));
    }
}
