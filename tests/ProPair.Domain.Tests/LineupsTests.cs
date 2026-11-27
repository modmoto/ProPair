using ProPair.Domain.Common;
using ProPair.Domain.Tournaments;

namespace ProPair.Domain.Tests;

public class LineupsTests
{
    private static RegisteredTeam Team(string name, int players, int? members = null) => new(name, members ?? players,
        Enumerable.Range(1, players).Select(i => new Player(PlayerId.New(), $"{name}{i}", "ogre-khans", "list")).ToList());

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void Complete_teams_are_taken_as_they_are(int size)
    {
        var lineups = Lineups.Build([Team("A", size), Team("B", size, members: size + 2)])!;

        Assert.All(lineups, l => Assert.Equal(size, l.Roster.Count));
        Assert.All(lineups, l => Assert.Empty(l.LeftOut!));
        Assert.DoesNotContain(lineups.SelectMany(l => l.Roster), p => p.IsDummy);
    }

    [Fact]
    public void Missing_players_become_dummies()
    {
        var lineups = Lineups.Build([Team("A", 4), Team("B", 4), Team("C", 2, members: 4), Team("D", 0, members: 4)])!;

        Assert.Equal(["C1", "C2", "Dummy", "Dummy 2"], lineups[2].Roster.Select(p => p.Name));
        Assert.Equal([false, false, true, true], lineups[2].Roster.Select(p => p.IsDummy));
        Assert.All(lineups[3].Roster, p => Assert.True(p.IsDummy));
    }

    [Fact]
    public void One_team_with_a_reserve_does_not_change_the_team_size()
    {
        var lineups = Lineups.Build([Team("A", 3), Team("B", 4), Team("C", 3)])!;

        Assert.All(lineups, l => Assert.Equal(3, l.Roster.Count));
        Assert.Equal(["B4"], lineups[1].LeftOut);
    }

    [Fact]
    public void Judges_without_a_list_are_no_team()
    {
        var lineups = Lineups.Build([Team("A", 3), Team("B", 3), Team("Judge", 0, members: 1)])!;

        Assert.Equal(["A", "B"], lineups.Select(l => l.Name));
    }

    [Fact]
    public void Two_player_teams_are_not_supported() =>
        Assert.Null(Lineups.Build([Team("A", 2), Team("B", 2), Team("C", 3)]));

    [Fact]
    public void Without_any_list_the_rosters_stay_empty()
    {
        var lineups = Lineups.Build([Team("A", 0, members: 5), Team("B", 0, members: 1)])!;

        Assert.All(lineups, l => Assert.Empty(l.Roster));
    }
}
