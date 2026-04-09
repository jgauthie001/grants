using Grants.Models.Fighter;
using Grants.Models.Cards;
using Grants.Models.Match;

namespace Grants.Engine;

/// <summary>
/// Simple AI engine for PvE opponents. Selects a card pair for the AI fighter each round.
/// Strategy: greedy — picks the pair with highest combined power that is in range.
/// Can be subclassed or extended per fighter for unique AI behavior.
/// </summary>
public static class AiEngine
{
    /// <summary>
    /// Select a card pair for the AI fighter.
    /// Basic strategy: prioritize getting into range, then highest power.
    /// </summary>
    public static Models.Cards.CardPair SelectPair(
        FighterInstance ai,
        FighterInstance opponent,
        Models.Board.HexBoard board)
    {
        int distance = new Models.Board.HexCoord(ai.HexQ, ai.HexR)
            .DistanceTo(new Models.Board.HexCoord(opponent.HexQ, opponent.HexR));

        var validPairs = ai.GetValidPairs();
        if (validPairs.Count == 0)
            throw new InvalidOperationException($"AI fighter {ai.DisplayName} has no valid card pairs.");

        // If out of range on all attack options, prefer a movement-heavy pair
        bool anyInRange = validPairs.Any(p => p.IsInRange(distance));
        if (!anyInRange)
        {
            // Pick pair with most movement
            return validPairs.OrderByDescending(p => p.CombinedMovement).First();
        }

        // Among in-range pairs, pick highest power
        return validPairs
            .Where(p => p.IsInRange(distance))
            .OrderByDescending(p => p.CombinedPower)
            .ThenByDescending(p => p.CombinedSpeed)
            .First();
    }
}
