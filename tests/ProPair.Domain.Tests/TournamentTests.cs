using ProPair.Domain.Common;
using ProPair.Domain.Preparations;
using ProPair.Domain.Ratings;
using ProPair.Domain.Teams;
using ProPair.Domain.Tournaments;

namespace ProPair.Domain.Tests;

public class TournamentTests
{
    private static List<Player> Roster(int n) =>
        Enumerable.Range(1, n).Select(i => new Player(PlayerId.New(), $"P{i}", "ogre-khans", "")).ToList();

    private static Tournament NewTournament(int size = 3) =>
        new(TournamentId.New(), "x", "Cup", "t9a", new TeamSize(size), null,
        [
            new Participant(ParticipantId.New(), "A", Roster(size)),
            new Participant(ParticipantId.New(), "B", Roster(size)),
        ]);

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(8)]
    public void TeamSize_accepts_supported_sizes(int size) => Assert.Equal(size, new TeamSize(size).Value);

    [Theory]
    [InlineData(2)]
    [InlineData(9)]
    public void TeamSize_rejects_unsupported(int size) => Assert.Throws<DomainException>(() => new TeamSize(size));

    [Fact]
    public void Roster_has_at_most_eight_players() =>
        Assert.Throws<DomainException>(() => new Tournament(TournamentId.New(), "x", "Cup", "t9a", null, null,
            [new Participant(ParticipantId.New(), "A", Roster(9))]));

    [Fact]
    public void Team_size_follows_the_largest_roster()
    {
        var t = new Tournament(TournamentId.New(), "x", "Cup", "t9a", null, null,
            [new Participant(ParticipantId.New(), "A", Roster(5)), new Participant(ParticipantId.New(), "B", [])]);
        Assert.Equal(5, t.TeamSize?.Value);
    }

    [Fact]
    public void Changed_list_gives_a_new_player_id()
    {
        var t = NewTournament();
        var a = t.Participants[0];
        var now = new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.Zero);
        var before = a.Roster.Select(p => p.Id).ToList();
        var roster = a.Roster.Select((p, i) => i == 0 ? p with { ListText = "Ogre Khans\n500 - Khan" } : p).ToList();

        t.Update("Cup", null, null, true, [new(a.Name, roster)], now);

        Assert.NotEqual(before[0], t.Participants[0].Roster[0].Id);
        Assert.Equal(now, t.Participants[0].Roster[0].ListChangedAt);
        Assert.Equal(before[1], t.Participants[0].Roster[1].Id);
        Assert.Null(t.Participants[0].Roster[1].ListChangedAt);
    }

    [Fact]
    public void Footer_changes_do_not_count_as_a_new_list() =>
        Assert.True(ArmyList.Same("Ogre Khans\n500 - Khan\nCreated with newrecruit.eu v1", "Ogre Khans\n500 - Khan  \n"));

    [Fact]
    public void Hidden_lists_keep_the_rosters()
    {
        var t = NewTournament();
        var ids = t.Participants[0].Roster.Select(p => p.Id).ToList();

        t.Update("Cup", null, null, false, [new("A", []), new("B", []), new("C", [])], DateTimeOffset.UnixEpoch);

        Assert.False(t.ListsVisible);
        Assert.Equal(ids, t.Participants[0].Roster.Select(p => p.Id));
        Assert.Empty(t.Participants.Single(p => p.Name == "C").Roster);
    }

    [Fact]
    public void Update_keeps_player_ids_for_unchanged_players()
    {
        var t = NewTournament();
        var a = t.Participants[0];
        var keptId = a.Roster[0].Id;
        var newRoster = a.Roster.Select(p => p with { Id = PlayerId.New() }).ToList();

        t.Update("Cup", null, [new(a.Name, newRoster)]);

        Assert.Equal(keptId, t.Participants[0].Roster[0].Id);
    }

    [Fact]
    public void Dummies_keep_their_id_until_the_player_arrives()
    {
        var t = NewTournament();
        var players = t.Participants[0].Roster.Take(2).ToList();
        t.Update("Cup", null, [new("A", [.. players, Player.Dummy(1)])]);
        var dummy = t.Participants[0].Roster[2].Id;
        Assert.True(t.Participants[0].IsIncomplete);

        t.Update("Cup", null, [new("A", [.. players, Player.Dummy(1)])]);
        Assert.Equal(dummy, t.Participants[0].Roster[2].Id);

        t.Update("Cup", null, [new("A", [.. players, new Player(PlayerId.New(), "Late", "ogre-khans", "list")])]);
        Assert.NotEqual(dummy, t.Participants[0].Roster[2].Id);
        Assert.False(t.Participants[0].IsIncomplete);
    }

    [Fact]
    public void Preparation_copies_team_scale_and_resolves_access()
    {
        var team = Team.Create("A");
        var t = NewTournament();
        var prep = Preparation.Start(team, t, t.Participants[0].Id);

        team.ChangeDefaultRatingScale(new RatingScale([new("a", "A", "#000", 1), new("b", "B", "#fff", 0)]));

        Assert.Equal(RatingScale.Default, prep.RatingScale);
        Assert.Equal(AccessLevel.Edit, prep.AccessFor(prep.EditToken.Value));
        Assert.Equal(AccessLevel.Read, prep.AccessFor(prep.ReadToken.Value));
        Assert.Equal(AccessLevel.None, prep.AccessFor("nope"));
    }
}
