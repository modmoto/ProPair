namespace ProPair.Domain.Pairing.Advice;

/// <summary>
/// Exact Nash equilibrium (mixed strategies) of a zero-sum matrix game – the right model for moves
/// both teams make hidden and at the same time. Neither side can do better by deviating; the result
/// lies between the pessimistic (we commit first) and optimistic (they commit first) values.
/// Pure saddle points and 2×2 games are solved in closed form, everything else with a small simplex.
/// </summary>
public sealed class EquilibriumSolver : IMatrixGameSolver
{
    private const double Eps = 1e-9;

    public double Value(double[,] m, int rows, int cols)
    {
        if (TrySaddle(m, rows, cols, out var value, out _, out _)) return value;
        if (rows == 2 && cols == 2) return Solve2X2(m).Value;
        return Simplex(m, rows, cols).Value;
    }

    public MatrixGameSolution Solve(double[,] m, int rows, int cols)
    {
        if (TrySaddle(m, rows, cols, out var value, out var row, out var col))
        {
            var r = new double[rows];
            var c = new double[cols];
            r[row] = 1;
            c[col] = 1;
            return MatrixGameSolution.FromMixes(value, r, c, m);
        }
        var mixed = rows == 2 && cols == 2 ? Solve2X2(m) : Simplex(m, rows, cols);
        return MatrixGameSolution.FromMixes(mixed.Value, mixed.Rows, mixed.Cols, m);
    }

    /// <summary>Pure equilibrium: our best guaranteed row equals their best guaranteed column.</summary>
    private static bool TrySaddle(double[,] m, int rows, int cols, out double value, out int bestRow, out int bestCol)
    {
        double lower = double.NegativeInfinity, upper = double.PositiveInfinity;
        bestRow = bestCol = 0;
        for (var r = 0; r < rows; r++)
        {
            var min = double.PositiveInfinity;
            for (var c = 0; c < cols; c++) min = Math.Min(min, m[r, c]);
            if (min > lower) { lower = min; bestRow = r; }
        }
        for (var c = 0; c < cols; c++)
        {
            var max = double.NegativeInfinity;
            for (var r = 0; r < rows; r++) max = Math.Max(max, m[r, c]);
            if (max < upper) { upper = max; bestCol = c; }
        }
        value = lower;
        return upper - lower < Eps;
    }

    /// <summary>2×2 without saddle point: both sides mix so the other is indifferent.</summary>
    private static (double Value, double[] Rows, double[] Cols) Solve2X2(double[,] m)
    {
        double a = m[0, 0], b = m[0, 1], c = m[1, 0], d = m[1, 1];
        var denominator = a - b - c + d;
        var p = (d - c) / denominator;   // probability we play row 0
        var q = (d - b) / denominator;   // probability they play column 0
        var value = (a * d - b * c) / denominator;
        return (value, [p, 1 - p], [q, 1 - q]);
    }

    /// <summary>
    /// Shift the payoffs positive, then solve: maximise Σy subject to A·y ≤ 1, y ≥ 0 (column player's LP).
    /// Value = 1/Σy; the column mix is y·value, the row mix comes from the dual (objective row under the slacks).
    /// Bland's rule prevents cycling on degenerate games.
    /// </summary>
    private static (double Value, double[] Rows, double[] Cols) Simplex(double[,] payoff, int rows, int cols)
    {
        var min = double.PositiveInfinity;
        for (var r = 0; r < rows; r++)
        for (var c = 0; c < cols; c++)
            min = Math.Min(min, payoff[r, c]);
        var shift = 1 - min; // all entries ≥ 1

        var width = cols + rows + 1;
        var t = new double[rows + 1, width];
        var basis = new int[rows];
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++) t[r, c] = payoff[r, c] + shift;
            t[r, cols + r] = 1;
            t[r, width - 1] = 1;
            basis[r] = cols + r;
        }
        for (var c = 0; c < cols; c++) t[rows, c] = -1;

        while (true)
        {
            var enter = -1;
            for (var c = 0; c < width - 1; c++)
                if (t[rows, c] < -Eps) { enter = c; break; }
            if (enter < 0) break;

            var leave = -1;
            var bestRatio = double.PositiveInfinity;
            for (var r = 0; r < rows; r++)
            {
                if (t[r, enter] <= Eps) continue;
                var ratio = t[r, width - 1] / t[r, enter];
                if (ratio < bestRatio - Eps || (Math.Abs(ratio - bestRatio) <= Eps && basis[r] < basis[leave]))
                {
                    bestRatio = ratio;
                    leave = r;
                }
            }

            var pivot = t[leave, enter];
            for (var c = 0; c < width; c++) t[leave, c] /= pivot;
            for (var r = 0; r <= rows; r++)
            {
                if (r == leave || Math.Abs(t[r, enter]) <= Eps) continue;
                var factor = t[r, enter];
                for (var c = 0; c < width; c++) t[r, c] -= factor * t[leave, c];
            }
            basis[leave] = enter;
        }

        var sum = t[rows, width - 1];
        var gameValue = 1 / sum;
        var colMix = new double[cols];
        for (var r = 0; r < rows; r++)
            if (basis[r] < cols) colMix[basis[r]] = t[r, width - 1] * gameValue;
        var rowMix = new double[rows];
        for (var r = 0; r < rows; r++) rowMix[r] = t[rows, cols + r] * gameValue;

        return (gameValue - shift, rowMix, colMix);
    }
}
