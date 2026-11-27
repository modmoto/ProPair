namespace ProPair.Domain.Pairing.Advice;

/// <summary>
/// Maximin with pure moves: we commit first and the opponent answers knowing our move.
/// A guaranteed lower bound – safe, but too pessimistic for hidden simultaneous moves.
/// </summary>
public sealed class PessimisticSolver : IMatrixGameSolver
{
    public double Value(double[,] m, int rows, int cols)
    {
        var best = double.NegativeInfinity;
        for (var r = 0; r < rows; r++)
        {
            var worst = double.PositiveInfinity;
            for (var c = 0; c < cols && worst > best; c++) worst = Math.Min(worst, m[r, c]);
            best = Math.Max(best, worst);
        }
        return best;
    }

    public MatrixGameSolution Solve(double[,] m, int rows, int cols)
    {
        var value = Value(m, rows, cols);
        var rowMix = new double[rows];
        var bestRows = Enumerable.Range(0, rows).Where(r => RowMin(m, r, cols) >= value - 1e-9).ToList();
        foreach (var r in bestRows) rowMix[r] = 1.0 / bestRows.Count;

        // opponent: best answer to our first best row
        var colMix = new double[cols];
        var row = bestRows[0];
        var answers = Enumerable.Range(0, cols).Where(c => m[row, c] <= value + 1e-9).ToList();
        foreach (var c in answers) colMix[c] = 1.0 / answers.Count;

        // they see our move: a row is worth its minimum; we see theirs: a column is worth its maximum
        var rowValues = Enumerable.Range(0, rows).Select(r => RowMin(m, r, cols)).ToArray();
        var colValues = Enumerable.Range(0, cols).Select(c => Enumerable.Range(0, rows).Max(r => m[r, c])).ToArray();
        return new MatrixGameSolution(value, rowMix, colMix, rowValues, colValues);
    }

    private static double RowMin(double[,] m, int r, int cols)
    {
        var min = double.PositiveInfinity;
        for (var c = 0; c < cols; c++) min = Math.Min(min, m[r, c]);
        return min;
    }
}
