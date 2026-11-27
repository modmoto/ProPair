namespace ProPair.Domain.Pairing.Advice;

/// <summary>
/// Solves one simultaneous step as a zero-sum matrix game: rows are our moves (we maximise),
/// columns are the opponent's moves (they minimise our score).
/// </summary>
public interface IMatrixGameSolver
{
    /// <summary>Value of the game. Hot path – called millions of times, must not allocate.</summary>
    double Value(double[,] payoff, int rows, int cols);

    /// <summary>Value plus how often each side plays each move.</summary>
    MatrixGameSolution Solve(double[,] payoff, int rows, int cols);
}

/// <param name="Rows">How often we play each row.</param>
/// <param name="Cols">How often the opponent plays each column.</param>
/// <param name="RowValues">What each of our rows is worth under this solver's model of the opponent.</param>
/// <param name="ColValues">What each opponent column is worth (our view) under this solver's model of us.</param>
public sealed record MatrixGameSolution(double Value, double[] Rows, double[] Cols, double[] RowValues, double[] ColValues)
{
    /// <summary>Expected values when both sides play the given mixes (Nash: each option against the other side's mix).</summary>
    public static MatrixGameSolution FromMixes(double value, double[] rows, double[] cols, double[,] m)
    {
        var rowValues = new double[rows.Length];
        var colValues = new double[cols.Length];
        for (var r = 0; r < rows.Length; r++)
        for (var c = 0; c < cols.Length; c++)
        {
            rowValues[r] += cols[c] * m[r, c];
            colValues[c] += rows[r] * m[r, c];
        }
        return new MatrixGameSolution(value, rows, cols, rowValues, colValues);
    }
}
