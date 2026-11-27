using ProPair.Domain.Common;
using ProPair.Domain.Matrices;
using ProPair.Domain.Preparations;
using ProPair.Domain.Ratings;
using ProPair.Domain.Teams;
using ProPair.Domain.Tournaments;

namespace ProPair.Domain.Tests;

public class NoteTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Cell_note_is_set_trimmed_and_removed_by_empty_text()
    {
        var own = PlayerId.New();
        var opp = PlayerId.New();
        var cell = new CellKey(own, opp);
        var m = Matrix.Create(PreparationId.New(), ParticipantId.New(), [own], [opp]);

        m.SetNote(cell, "  Achtung auf Kanone  ", "Simon", Now);
        Assert.Equal(new Note("Achtung auf Kanone", "Simon", Now), m.NoteAt(cell));

        m.SetNote(cell, " ", "Simon", Now);
        Assert.Null(m.NoteAt(cell));
    }

    [Fact]
    public void Cell_note_rejects_foreign_cell_and_too_long_text()
    {
        var own = PlayerId.New();
        var opp = PlayerId.New();
        var m = Matrix.Create(PreparationId.New(), ParticipantId.New(), [own], [opp]);

        Assert.Throws<DomainException>(() => m.SetNote(new CellKey(PlayerId.New(), opp), "x", "Simon", Now));
        Assert.Throws<DomainException>(() => m.SetNote(new CellKey(own, opp), new string('x', Note.MaxLength + 1), "Simon", Now));
    }

    [Fact]
    public void Cell_history_filters_by_cell()
    {
        var own = PlayerId.New();
        var opp1 = PlayerId.New();
        var opp2 = PlayerId.New();
        var m = Matrix.Create(PreparationId.New(), ParticipantId.New(), [own], [opp1, opp2]);

        m.SetRating(new CellKey(own, opp1), "plus", "A", RatingScale.Default, Now);
        m.SetRating(new CellKey(own, opp2), "minus", "B", RatingScale.Default, Now);
        m.SetRating(new CellKey(own, opp1), "zero", "C", RatingScale.Default, Now);

        Assert.Equal(["A", "C"], m.HistoryOf(new CellKey(own, opp1)).Select(h => h.Author));
    }

    [Fact]
    public void Player_note_lives_on_preparation()
    {
        var players = Enumerable.Range(0, 3).Select(i => new Player(PlayerId.New(), $"P{i}", "ogre-khans", "")).ToList();
        var t = new Tournament(TournamentId.New(), "x", "Cup", "t9a", new TeamSize(3), null,
            [new Participant(ParticipantId.New(), "A", players)]);
        var prep = Preparation.Start(Team.Create("A"), t, t.Participants[0].Id);

        prep.SetPlayerNote(players[0].Id, "spielt 2 Kanonen", "Simon", Now);

        Assert.Equal("spielt 2 Kanonen", prep.PlayerNotes[players[0].Id].Text);
        prep.SetPlayerNote(players[0].Id, "", "Simon", Now);
        Assert.Empty(prep.PlayerNotes);
    }
}
