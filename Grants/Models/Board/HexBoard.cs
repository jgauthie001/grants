namespace Grants.Models.Board;

/// <summary>
/// The 7x7 hexagonal fight board. Uses offset coordinates internally
/// but works with HexCoord (axial) for logic.
/// Fighter A starts on the left side, Fighter B on the right.
/// </summary>
public class HexBoard
{
    public const int Width = 7;
    public const int Height = 7;

    // Default starting positions (axial coords on a 7x7 offset grid)
    public static readonly HexCoord FighterAStart = new(-3, 0);
    public static readonly HexCoord FighterBStart = new(3, 0);

    private readonly HashSet<HexCoord> _validCells;

    public HexBoard()
    {
        _validCells = GenerateGridCells();
    }

    private static HashSet<HexCoord> GenerateGridCells()
    {
        // Generate a 7x7 hex grid using axial coordinates
        // Center at (0,0), radius 3
        var cells = new HashSet<HexCoord>();
        int radius = 3;
        for (int q = -radius; q <= radius; q++)
        {
            int rMin = Math.Max(-radius, -q - radius);
            int rMax = Math.Min(radius, -q + radius);
            for (int r = rMin; r <= rMax; r++)
                cells.Add(new HexCoord(q, r));
        }
        return cells;
    }

    public bool IsValid(HexCoord coord) => _validCells.Contains(coord);

    // Occupied is tracked externally (match state)
    private readonly HashSet<HexCoord> _occupied = new();

    public bool IsOccupied(HexCoord coord) => _occupied.Contains(coord);

    public void SetOccupied(HexCoord coord, bool occupied)
    {
        if (occupied) _occupied.Add(coord);
        else _occupied.Remove(coord);
    }

    public IReadOnlyCollection<HexCoord> AllCells => _validCells;

    /// <summary>Returns the pixel center of a hex cell for rendering (pointy-top orientation).</summary>
    public static (float X, float Y) HexToPixel(HexCoord coord, float hexSize, float originX, float originY)
    {
        float x = hexSize * (MathF.Sqrt(3) * coord.Q + MathF.Sqrt(3) / 2f * coord.R);
        float y = hexSize * (3f / 2f * coord.R);
        return (originX + x, originY + y);
    }

    /// <summary>Returns which hex contains a screen pixel (for mouse input).</summary>
    public static HexCoord PixelToHex(float px, float py, float hexSize, float originX, float originY)
    {
        float x = (px - originX) / hexSize;
        float y = (py - originY) / hexSize;
        float q = (MathF.Sqrt(3) / 3f * x - 1f / 3f * y);
        float r = (2f / 3f * y);
        return RoundHex(q, r);
    }

    private static HexCoord RoundHex(float q, float r)
    {
        float s = -q - r;
        int rq = (int)MathF.Round(q);
        int rr = (int)MathF.Round(r);
        int rs = (int)MathF.Round(s);
        float dq = MathF.Abs(rq - q);
        float dr = MathF.Abs(rr - r);
        float ds = MathF.Abs(rs - s);
        if (dq > dr && dq > ds) rq = -rr - rs;
        else if (dr > ds) rr = -rq - rs;
        return new HexCoord(rq, rr);
    }
}
