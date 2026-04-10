using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;

namespace Grants.Engine;

/// <summary>
/// Handles positioning: applies movement from a CardPair to a FighterInstance.
/// Movement is relational to the opponent:
///   Approach — moves toward the opponent (min..max hexes closer)
///   Retreat  — moves away from the opponent (min..max hexes farther)
///   Free     — player picks any hex within min..max steps
///   None     — card is stationary; no repositioning
///
/// Movement range:
///   EffectiveMinMovement > 0 means movement is mandatory (can't stay put).
///   EffectiveMaxMovement == 0 means no movement occurs regardless of type.
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
        var movingCard = pair.Unique ?? (CardBase?)pair.Special;
        int maxMovement = (pair.Generic != null ? mover.GetCardMovement(pair.Generic) : 0)
            + (movingCard != null ? mover.GetCardMovement(movingCard) : 0);
        int minMovement = pair.EffectiveMinMovement;

        var movementType = pair.CombinedMovementType;

        if (maxMovement <= 0 || movementType == MovementType.None) return currentPos;

        bool approaching = movementType == MovementType.Approach;

        // Free movement: player picks any hex in [min, max] range
        if (movementType == MovementType.Free)
        {
            if (chosenDestination.HasValue)
            {
                var dest = chosenDestination.Value;
                int dist = currentPos.DistanceTo(dest);
                if (board.IsValid(dest) && !board.IsOccupied(dest) && dest != opponentPos
                    && dist >= minMovement && dist <= maxMovement)
                    return dest;
            }
            // Auto: move toward opponent respecting min/max
            return BestCandidate(currentPos, opponentPos, minMovement, maxMovement, approachTarget: true, board);
        }

        // Directional: validate chosen destination matches movement type and range
        if (chosenDestination.HasValue)
        {
            var dest = chosenDestination.Value;
            int distFromOrigin = currentPos.DistanceTo(dest);
            if (board.IsValid(dest) && !board.IsOccupied(dest) && dest != opponentPos
                && distFromOrigin >= minMovement && distFromOrigin <= maxMovement)
            {
                int currentDist = currentPos.DistanceTo(opponentPos);
                int destDist = dest.DistanceTo(opponentPos);
                bool validDirection = approaching ? destDist <= currentDist : destDist >= currentDist;
                if (validDirection)
                    return dest;
            }
        }

        return BestCandidate(currentPos, opponentPos, minMovement, maxMovement, approaching, board);
    }

    /// <summary>
    /// Picks the best hex within [minSteps, maxSteps] of origin.
    /// Closest to opponent if approaching, farthest if retreating.
    /// Falls back to any reachable hex if no hex satisfies the min constraint.
    /// </summary>
    private static HexCoord BestCandidate(
        HexCoord origin, HexCoord opponentPos, int minSteps, int maxSteps, bool approachTarget, HexBoard board)
    {
        var all = HexMath.ReachableHexes(origin, maxSteps, board);
        all.Remove(opponentPos);
        if (all.Count == 0) return origin;

        // Try to satisfy min constraint first
        var valid = all.Where(c => origin.DistanceTo(c) >= minSteps).ToList();
        var pool = valid.Count > 0 ? valid : all; // fall back if nothing meets min

        return approachTarget
            ? pool.OrderBy(c => c.DistanceTo(opponentPos)).First()
            : pool.OrderByDescending(c => c.DistanceTo(opponentPos)).First();
    }

    /// <summary>
    /// Returns all hexes a fighter can legally reach given this pair's movement range and type.
    /// Used by FightScreen to highlight valid destination choices.
    /// </summary>
    public static List<HexCoord> GetReachableHexes(
        FighterInstance mover,
        CardPair pair,
        HexCoord currentPos,
        HexCoord opponentPos,
        HexBoard board)
    {
        var movingCard2 = pair.Unique ?? (CardBase?)pair.Special;
        int maxMovement = (pair.Generic != null ? mover.GetCardMovement(pair.Generic) : 0)
            + (movingCard2 != null ? mover.GetCardMovement(movingCard2) : 0);
        int minMovement = pair.EffectiveMinMovement;

        var movementType = pair.CombinedMovementType;

        if (maxMovement <= 0 || movementType == MovementType.None)
            return new List<HexCoord>();

        var all = HexMath.ReachableHexes(currentPos, maxMovement, board);
        all.Remove(opponentPos);

        int currentDist = currentPos.DistanceTo(opponentPos);

        // Apply min distance from origin
        if (minMovement > 0)
            all = all.Where(c => currentPos.DistanceTo(c) >= minMovement).ToList();

        return movementType switch
        {
            MovementType.Approach => all.Where(c => c.DistanceTo(opponentPos) <= currentDist).ToList(),
            MovementType.Retreat  => all.Where(c => c.DistanceTo(opponentPos) >= currentDist).ToList(),
            _                     => all, // Free
        };
    }
}
