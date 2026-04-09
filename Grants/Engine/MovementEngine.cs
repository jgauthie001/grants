using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;

namespace Grants.Engine;

/// <summary>
/// Handles positioning: applies movement from a CardPair to a FighterInstance,
/// validates against board bounds, and checks if they end up occupying the same hex.
/// Called before attack resolution (faster fighter moves first).
/// </summary>
public static class MovementEngine
{
    /// <summary>
    /// Apply movement from a card pair to a fighter. Returns the new position.
    /// Does not commit position — caller updates FighterInstance after both moves are processed
    /// to handle collision/blocking correctly.
    /// </summary>
    public static HexCoord ResolveMovement(
        FighterInstance mover,
        CardPair pair,
        HexCoord currentPos,
        HexCoord opponentPos,
        HexBoard board,
        HexCoord? chosenDestination = null)
    {
        int movement = mover.GetCardMovement(pair.Generic!) + (pair.Unique?.BaseMovement ?? pair.Special?.BaseMovement ?? 0);

        if (movement <= 0) return currentPos;

        // If player chose a destination explicitly, use it if valid
        if (chosenDestination.HasValue)
        {
            var dest = chosenDestination.Value;
            if (board.IsValid(dest) && !board.IsOccupied(dest) && dest != opponentPos)
                return dest;
            // Chosen hex is blocked (e.g. opponent moved there) — fall through to auto-move
        }

        // Determine direction: default is toward opponent unless Retreat keyword present
        bool retreating = pair.AllKeywords.ContainsKeyword(CardKeyword.Retreat);
        HexCoord direction = retreating
            ? currentPos - opponentPos
            : opponentPos - currentPos;

        // Normalize direction to one step
        int dist = currentPos.DistanceTo(opponentPos);
        if (dist == 0) return currentPos;

        var candidates = GetMovementCandidates(currentPos, movement, opponentPos, retreating, board);

        // Pick best candidate: closest to opponent (or farthest if retreating)
        if (candidates.Count == 0) return currentPos;

        HexCoord best = retreating
            ? candidates.OrderByDescending(c => c.DistanceTo(opponentPos)).First()
            : candidates.OrderBy(c => c.DistanceTo(opponentPos)).First();

        // Cannot move onto opponent's hex
        if (best == opponentPos) best = currentPos;

        return best;
    }

    private static List<HexCoord> GetMovementCandidates(
        HexCoord origin, int steps, HexCoord target, bool retreating, HexBoard board)
    {
        var reachable = HexMath.ReachableHexes(origin, steps, board);
        // Remove opponent hex
        reachable.Remove(target);
        return reachable;
    }
}
