using ProPair.Domain.Pairing.Advice;

namespace ProPair.Domain.Tests;

public class MatrixGameSolverTests
{
    private readonly EquilibriumSolver _nash = new();

    private static double[,] M(double[][] rows)
    {
        var m = new double[rows.Length, rows[0].Length];
        for (var r = 0; r < rows.Length; r++)
        for (var c = 0; c < rows[0].Length; c++)
            m[r, c] = rows[r][c];
        return m;
    }

    [Fact]
    public void Rock_paper_scissors_is_fair_and_uniform()
    {
        var s = _nash.Solve(M([[0, -1, 1], [1, 0, -1], [-1, 1, 0]]), 3, 3);

        Assert.Equal(0, s.Value, 9);
        Assert.All(s.Rows, p => Assert.Equal(1 / 3.0, p, 6));
        Assert.All(s.Cols, q => Assert.Equal(1 / 3.0, q, 6));
    }

    [Fact]
    public void Saddle_point_is_played_pure()
    {
        var s = _nash.Solve(M([[3, 1], [4, 2]]), 2, 2); // row 1 dominates, column 1 is their answer

        Assert.Equal(2, s.Value, 9);
        Assert.Equal([0.0, 1.0], s.Rows);
        Assert.Equal([0.0, 1.0], s.Cols);
    }

    [Fact]
    public void Matching_pennies_2x2_mixes_half_half()
    {
        var s = _nash.Solve(M([[1, -1], [-1, 1]]), 2, 2);

        Assert.Equal(0, s.Value, 9);
        Assert.Equal(0.5, s.Rows[0], 9);
        Assert.Equal(0.5, s.Cols[0], 9);
    }

    /// <summary>
    /// Defining property of an equilibrium: our mix guarantees at least the value against every column,
    /// their mix holds us to at most the value against every row. Checked on many random games.
    /// </summary>
    [Theory]
    [InlineData(2, 3)]
    [InlineData(3, 3)]
    [InlineData(6, 6)]
    [InlineData(10, 10)]
    [InlineData(21, 21)]
    [InlineData(5, 9)]
    public void Random_games_satisfy_equilibrium_conditions(int rows, int cols)
    {
        var rnd = new Random(rows * 100 + cols);
        for (var run = 0; run < 25; run++)
        {
            var m = new double[rows, cols];
            for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                m[r, c] = rnd.Next(-6, 7); // integer sums like real matchup totals, many ties

            var s = _nash.Solve(m, rows, cols);

            Assert.Equal(1, s.Rows.Sum(), 6);
            Assert.Equal(1, s.Cols.Sum(), 6);
            Assert.All(s.Rows.Concat(s.Cols), p => Assert.True(p >= -1e-9));
            for (var c = 0; c < cols; c++)
                Assert.True(Enumerable.Range(0, rows).Sum(r => s.Rows[r] * m[r, c]) >= s.Value - 1e-6);
            for (var r = 0; r < rows; r++)
                Assert.True(Enumerable.Range(0, cols).Sum(c => s.Cols[c] * m[r, c]) <= s.Value + 1e-6);
            Assert.Equal(s.Value, _nash.Value(m, rows, cols), 6);
        }
    }

    [Fact]
    public void Pessimistic_is_maximin()
    {
        var m = M([[0, -1, 1], [1, 0, -1], [-1, 1, 0]]);
        Assert.Equal(-1, new PessimisticSolver().Value(m, 3, 3));
    }
}
