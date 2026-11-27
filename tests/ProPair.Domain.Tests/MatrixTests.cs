using ProPair.Domain.Common;
using ProPair.Domain.Matrices;
using ProPair.Domain.Ratings;

namespace ProPair.Domain.Tests;

public class MatrixTests
{
    private static readonly PlayerId Own = PlayerId.New();
    private static readonly PlayerId Opp = PlayerId.New();
    private static readonly CellKey Cell = new(Own, Opp);
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    private static Matrix NewMatrix() =>
        Matrix.Create(PreparationId.New(), ParticipantId.New(), [Own], [Opp]);

    [Fact]
    public void SetRating_stores_value_and_records_history()
    {
        var m = NewMatrix();

        m.SetRating(Cell, "plus", "Simon", RatingScale.Default, Now);
        m.SetRating(Cell, "zero", "Anna", RatingScale.Default, Now);

        Assert.Equal("zero", m.RatingAt(Cell));
        Assert.Equal(2, m.History.Count);
        Assert.Equal(("plus", "zero", "Anna"), (m.History[1].OldLevelKey, m.History[1].NewLevelKey, m.History[1].Author));
        Assert.Equal(2, m.DomainEvents.Count);
    }

    [Fact]
    public void SetRating_null_clears_cell()
    {
        var m = NewMatrix();
        m.SetRating(Cell, "plus", "Simon", RatingScale.Default, Now);

        m.SetRating(Cell, null, "Simon", RatingScale.Default, Now);

        Assert.Null(m.RatingAt(Cell));
        Assert.Empty(m.Cells);
    }

    [Fact]
    public void SetRating_same_value_is_no_change()
    {
        var m = NewMatrix();
        m.SetRating(Cell, "plus", "Simon", RatingScale.Default, Now);
        m.SetRating(Cell, "plus", "Simon", RatingScale.Default, Now);

        Assert.Single(m.History);
    }

    [Fact]
    public void SetRating_rejects_level_outside_scale() =>
        Assert.Throws<DomainException>(() => NewMatrix().SetRating(Cell, "double-plus", "Simon", RatingScale.Default, Now));

    [Fact]
    public void SetRating_rejects_foreign_cell() =>
        Assert.Throws<DomainException>(() =>
            NewMatrix().SetRating(new CellKey(PlayerId.New(), Opp), "plus", "Simon", RatingScale.Default, Now));

    [Fact]
    public void SetRating_requires_author() =>
        Assert.Throws<DomainException>(() => NewMatrix().SetRating(Cell, "plus", " ", RatingScale.Default, Now));

    [Fact]
    public void SyncPlayers_drops_cells_of_removed_players()
    {
        var m = NewMatrix();
        m.SetRating(Cell, "plus", "Simon", RatingScale.Default, Now);
        var newOpp = PlayerId.New();

        var changed = m.SyncPlayers([Own], [newOpp]);

        Assert.True(changed);
        Assert.Empty(m.Cells);
        Assert.Single(m.History);
    }
}
