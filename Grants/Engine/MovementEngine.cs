using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;

namespace Grants.Engine;

/// <summary>
/// Handles positioning: applies movement from a CardPair to a FighterInstance.
/// Movement is relational to the opponent:
///   Approach — moves toward the opponent by up to N hexes
///   Retreat  — moves away from the opponent by up to N hexes
///   Free     — player picks any reachable hex (shown as highlighted board hexes)
///   None     — card is stationary; no repositioning
/// </summary>
public static class MovementEngine
{
    /// <summary>
    /// Apply movement from a card pair to a fighter. Returns the new position.
    /// Does not commit position — caller updates FighterInstance after both moves.
    /// </summary>
    public static HexCoord ResolveMovement(
        FighterInstance mover,
        CardPair pair,
        HexCoord currentPos,
        HexCoord opponentPos,
        HexBoard board,
        HexCoord? chosenDestination = null)
    {
        int movement = mover.GetCardMovement(pair.Generic ?? (CardBase?)pair.Special ?? pair.Generic!)
            + (pair.Unique?.BaseMovement ?? pair.Special?.BaseMovement ?? 0);

        var movementType = pair.CombinedMovementType;

        if (movement <= 0 || movementType == MovementType.None) return currentPos;

        // Free movement: player chooses; fall back to auto-approach if no choice given
        if (movementType == MovementType.Free)
        {
            if (chosenDestination.HasValue)
            {
                var dest = chosenDestination.Value;
                if (board.IsValid(dest) && !board.IsOccupied(dest) && dest != opponentPos)
                    return dest;
            }
            // Auto: move toward opponent
            return BestCandidate(currentPos, opponentPos, movement, approachTarget: true, board);
        }

        bool approaching = movementType == MovementType.Approach;

        // For explicit player choice on Approach/Retreat, validate direction intent
        if (chosenDestination.HasValue)
        {
            var dest = chosenDestination.Value;
            if (board.IsValid(dest) && !board.IsOccupied(dest) && dest != opponentPos)
            {
                int currentDist = currentPos.DistanceTo(opponentPos);
                int destDist = dest.DistanceTo(opponentPos);
                bool goesBetter = approaching ? destDist <= currentDist : destDist >= currentDist;
                if (goesBetter)
                    return dest;
            }
            // Chosen hex doesn't match movement intent — fall through to auto
        }

        return BestCandidate(currentPos, opponentPos, movement, approaching, board);
    }

    /// <summary>Computes all reachable hexes within range and picks closest (approach) or farthest (retreat).</summary>
    private static HexCoord BestCandidate(
        HexCoord origin, HexCoord opponentPos, int steps, bool approachTarget, HexBoard board)
    {
        var candidates = HexMath.ReachableHexes(origin, steps, board);
        candidates.Remove(opponentPos);
        if (candidates.Count == 0) return origin;

        return approachTarget
            ? candidates.OrderBy(c => c.DistanceTo(opponentPos)).First()
            : candidates.OrderByDescending(c => c.DistanceTo(opponentPos)).First();
    }

    /// <summary>
    /// Returns all hexes a fighter can legally reach given this pair's movement and type.
    /// Used by FightScreen to highlight valid destination choices for Free movement.
    /// </summary>
    public static List<HexCoord> GetReachableHexes(
        FighterInstance mover,
        CardPair pair,
        HexCoord currentPos,
        HexCoord opponentPos,
        HexBoard board)
    {
        int movement = mover.GetCardMovement(pair.Generic ?? (CardBase?)pair.Special ?? pair.Generic!)
            + (pair.Unique?.BaseMovement ?? pair.Special?.BaseMovement ?? 0);

        var movementType = pair.CombinedMovementType;

        if (movement <= 0 || movementType == MovementType.None)
            return new List<HexCoord>();

        var all = HexMath.ReachableHexes(currentPos, movement, board);
        all.Remove(opponentPos);

        int currentDist = currentPos.DistanceTo(opponentPos);

        return movementType switch
        {
            MovementType.Approach => all.Where(c => c.DistanceTo(opponentPos) <= currentDist).ToList(),
            MovementType.Retreat  => all.Where(c => c.DistanceTo(opponentPos) >= currentDist).ToList(),
            _                     => all, // Free
        };
    }
}
