namespace Grants.Models.Board;

/// <summary>
/// Cube coordinates for a hex grid cell. Using axial (q, r) projection.
/// s is derived: s = -q - r.
/// </summary>
public readonly struct HexCoord : IEquatable<HexCoord>
{
    public int Q { get; init; }
    public int R { get; init; }
    public int S => -Q - R;

    public HexCoord(int q, int r) { Q = q; R = r; }

    public static readonly HexCoord Zero = new(0, 0);

    // The 6 directional neighbors (pointy-top hex)
    public static readonly HexCoord[] Directions = new[]
    {
        new HexCoord( 1,  0), new HexCoord( 1, -1), new HexCoord( 0, -1),
        new HexCoord(-1,  0), new HexCoord(-1,  1), new HexCoord( 0,  1),
    };

    public HexCoord Neighbor(int direction) =>
        this + Directions[((direction % 6) + 6) % 6];

    public List<HexCoord> GetNeighbors() =>
        new() { Neighbor(0), Neighbor(1), Neighbor(2), Neighbor(3), Neighbor(4), Neighbor(5) };

    public int DistanceTo(HexCoord other) =>
        (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;

    public static HexCoord operator +(HexCoord a, HexCoord b) => new(a.Q + b.Q, a.R + b.R);
    public static HexCoord operator -(HexCoord a, HexCoord b) => new(a.Q - b.Q, a.R - b.R);
    public static bool operator ==(HexCoord a, HexCoord b) => a.Q == b.Q && a.R == b.R;
    public static bool operator !=(HexCoord a, HexCoord b) => !(a == b);
    public bool Equals(HexCoord other) => this == other;
    public override bool Equals(object? obj) => obj is HexCoord h && this == h;
    public override int GetHashCode() => HashCode.Combine(Q, R);
    public override string ToString() => $"({Q},{R})";
}

/// <summary>
/// Utility math for hex grid operations.
/// </summary>
public static class HexMath
{
    /// <summary>Returns all hexes within movement range of origin on the board.</summary>
    public static List<HexCoord> ReachableHexes(HexCoord origin, int movement, HexBoard board)
    {
        var visited = new HashSet<HexCoord> { origin };
        var frontier = new List<HexCoord> { origin };

        for (int step = 0; step < movement; step++)
        {
            var next = new List<HexCoord>();
            foreach (var hex in frontier)
            {
                for (int d = 0; d < 6; d++)
                {
                    var neighbor = hex.Neighbor(d);
                    if (!visited.Contains(neighbor) && board.IsValid(neighbor) && !board.IsOccupied(neighbor))
                    {
                        visited.Add(neighbor);
                        next.Add(neighbor);
                    }
                }
            }
            frontier = next;
        }

        visited.Remove(origin);
        return visited.ToList();
    }

    /// <summary>
    /// Returns the direction index (0-5) from origin toward target (approximate).
    /// </summary>
    public static int DirectionTo(HexCoord origin, HexCoord target)
    {
        var diff = target - origin;
        float angle = MathF.Atan2(diff.R, diff.Q);
        int dir = (int)MathF.Round(angle / (MathF.PI / 3f));
        return ((dir % 6) + 6) % 6;
    }
}
