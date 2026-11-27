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
    [InlineData(5)]
    [InlineData(8)]
    public void TeamSize_accepts_supported_sizes(int size) => Assert.Equal(size, new TeamSize(size).Value);

    [Fact]
    public void TeamSize_rejects_unsupported() => Assert.Throws<DomainException>(() => new TeamSize(4));

    [Fact]
    public void Roster_size_must_match_team_size() =>
        Assert.Throws<DomainException>(() => new Tournament(TournamentId.New(), "x", "Cup", "t9a", new TeamSize(3), null,
            [new Participant(ParticipantId.New(), "A", Roster(2))]));

    [Fact]
    public void Update_keeps_player_ids_for_unchanged_players()
    {
        var t = NewTournament();
        var a = t.Participants[0];
        var keptId = a.Roster[0].Id;
        var newRoster = a.Roster.Select(p => p with { Id = PlayerId.New() }).ToList();

        t.Update("Cup", null, [(a.Name, newRoster)]);

        Assert.Equal(keptId, t.Participants[0].Roster[0].Id);
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
