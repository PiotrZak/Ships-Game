using GameEngine.Models;

namespace GameEngine.Logic;

/// <summary>
/// ML-inspired AI using a probability-density Heat-Map with Hunt-and-Target strategy.
///
/// Algorithm overview:
///   1. For every remaining ship size, enumerate all valid horizontal and vertical
///      placements on the board (a placement is valid if none of its cells is a
///      confirmed miss).  Each cell in a valid placement receives +1 probability.
///   2. Cells that have already been shot are zeroed out.
///   3. When one or more hits exist whose ship has not yet been sunk (target mode),
///      the four direct neighbours of those hits receive a 3× probability boost,
///      directing the AI to finish off the wounded ship before hunting elsewhere.
///   4. The cell with the highest resulting score is chosen as the next shot.
///
/// This simple density-estimation approach mirrors techniques used in
/// Bayesian inference and tabular reinforcement learning.
/// </summary>
public class SmartAI
{
    private readonly int _boardRows;
    private readonly int _boardCols;
    private readonly double[,] _heatMap;
    private readonly bool[,] _shotFired;
    private readonly bool[,] _missedCell;
    private readonly List<(int row, int col)> _activeHits = new();
    private readonly List<int> _remainingLengths;

    /// <summary>Standard 10×10 board with full fleet.</summary>
    public SmartAI() : this(10, 10, Enum.GetValues<ShipClass>()) { }

    /// <summary>Custom board dimensions with full fleet.</summary>
    public SmartAI(int boardRows, int boardCols) : this(boardRows, boardCols, Enum.GetValues<ShipClass>()) { }

    /// <summary>Standard 10×10 board with a custom subset of ship classes.</summary>
    public SmartAI(IEnumerable<ShipClass> fleet) : this(10, 10, fleet) { }

    /// <summary>Fully customised constructor.</summary>
    public SmartAI(int boardRows, int boardCols, IEnumerable<ShipClass> fleet)
    {
        _boardRows = boardRows;
        _boardCols = boardCols;
        _heatMap = new double[boardRows + 1, boardCols + 1];
        _shotFired = new bool[boardRows + 1, boardCols + 1];
        _missedCell = new bool[boardRows + 1, boardCols + 1];
        _remainingLengths = fleet.Select(s => (int)s).ToList();
    }

    /// <summary>Records a successful hit at (row, col) using 1-based coordinates.</summary>
    public void RecordHit(int row, int col)
    {
        _shotFired[row, col] = true;
        _activeHits.Add((row, col));
    }

    /// <summary>Records a miss at (row, col) using 1-based coordinates.</summary>
    public void RecordMiss(int row, int col)
    {
        _shotFired[row, col] = true;
        _missedCell[row, col] = true;
    }

    /// <summary>
    /// Called when a ship of the given length has been sunk.
    /// Removes it from the remaining-fleet list and resets target mode.
    /// </summary>
    public void RecordSunk(int shipLength)
    {
        int idx = _remainingLengths.IndexOf(shipLength);
        if (idx >= 0) _remainingLengths.RemoveAt(idx);
        _activeHits.Clear();
    }

    /// <summary>Returns the best shot coordinate string (e.g. "A5").</summary>
    public string ChooseShot()
    {
        UpdateHeatMap();
        var (row, col) = FindBestCell();
        char rowChar = (char)('A' + row - 1);
        return $"{rowChar}{col}";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    private void UpdateHeatMap()
    {
        // Reset
        for (int r = 1; r <= _boardRows; r++)
            for (int c = 1; c <= _boardCols; c++)
                _heatMap[r, c] = 0;

        // Accumulate probability scores for all remaining ship sizes
        foreach (int len in _remainingLengths)
        {
            // Horizontal placements
            for (int r = 1; r <= _boardRows; r++)
                for (int c = 1; c <= _boardCols - len + 1; c++)
                    if (CanFit(r, c, len, horizontal: true))
                        for (int k = 0; k < len; k++)
                            _heatMap[r, c + k] += 1.0;

            // Vertical placements
            for (int r = 1; r <= _boardRows - len + 1; r++)
                for (int c = 1; c <= _boardCols; c++)
                    if (CanFit(r, c, len, horizontal: false))
                        for (int k = 0; k < len; k++)
                            _heatMap[r + k, c] += 1.0;
        }

        // Zero out cells that have already been shot
        for (int r = 1; r <= _boardRows; r++)
            for (int c = 1; c <= _boardCols; c++)
                if (_shotFired[r, c])
                    _heatMap[r, c] = 0;

        // Boost neighbours of un-sunk hits (target mode)
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };
        foreach (var (hr, hc) in _activeHits)
            for (int d = 0; d < 4; d++)
            {
                int nr = hr + dr[d], nc = hc + dc[d];
                if (nr >= 1 && nr <= _boardRows && nc >= 1 && nc <= _boardCols && !_shotFired[nr, nc])
                    _heatMap[nr, nc] *= 3.0;
            }
    }

    /// <summary>
    /// A placement is valid if none of its cells is a confirmed miss.
    /// (Hit cells are still candidates – they might be part of the same ship.)
    /// </summary>
    private bool CanFit(int startRow, int startCol, int len, bool horizontal)
    {
        for (int k = 0; k < len; k++)
        {
            int r = horizontal ? startRow : startRow + k;
            int c = horizontal ? startCol + k : startCol;
            if (_missedCell[r, c]) return false;
        }
        return true;
    }

    private (int row, int col) FindBestCell()
    {
        double max = -1;
        (int row, int col) best = (1, 1);
        for (int r = 1; r <= _boardRows; r++)
            for (int c = 1; c <= _boardCols; c++)
                if (!_shotFired[r, c] && _heatMap[r, c] > max)
                {
                    max = _heatMap[r, c];
                    best = (r, c);
                }
        return best;
    }
}
