using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;

namespace Grants.Engine;

/// <summary>
/// Orchestrates a full round of combat:
/// 1. Receive both committed card pairs
/// 2. Determine speed order
/// 3. Faster fighter: move then attack
/// 4. Slower fighter: move then attack (can be cancelled if disabled by step 3)
/// 5. Speed tie: simultaneous move+attack
/// 6. Process keywords, cooldowns
/// 7. Check KO
/// Returns a completed RoundState.
/// </summary>
public static class ResolutionEngine
{
    public static RoundState ResolveRound(MatchState match)
    {
        var pairA = match.SelectedPairA!;
        var pairB = match.SelectedPairB!;
        var fa = match.FighterA;
        var fb = match.FighterB;

        int speedA = fa.GetCardSpeed(pairA.Generic ?? (CardBase)pairA.Special!) +
                     fa.GetCardSpeed(pairA.Unique ?? (CardBase)pairA.Special!);
        int speedB = fb.GetCardSpeed(pairB.Generic ?? (CardBase)pairB.Special!) +
                     fb.GetCardSpeed(pairB.Unique ?? (CardBase)pairB.Special!);

        var round = new RoundState
        {
            RoundNumber = match.CurrentRound,
            PairA = pairA,
            PairB = pairB,
            SpeedA = speedA,
            SpeedB = speedB,
        };

        var posA = new HexCoord(fa.HexQ, fa.HexR);
        var posB = new HexCoord(fb.HexQ, fb.HexR);
        var chosenMoveA = match.ChosenMoveA;

        round.Log.Add($"--- Round {round.RoundNumber} ---");
        round.Log.Add($"Speed: {fa.DisplayName}={speedA}, {fb.DisplayName}={speedB}.");

        if (round.FighterAFaster)
        {
            // ===== FASTER: A moves then attacks =====
            match.Board.SetOccupied(posB, true);
            var newPosA = MovementEngine.ResolveMovement(fa, pairA, posA, posB, match.Board, chosenMoveA);
            match.Board.SetOccupied(posB, false);
            fa.HexQ = newPosA.Q; fa.HexR = newPosA.R;
            int distA = newPosA.DistanceTo(posB);
            round.Log.Add($"{fa.DisplayName} moves to {newPosA} (dist={distA}).");

            var resultA = AttackEngine.Resolve(fa, pairA, fb, pairB, distA, round);

            // ===== SLOWER: B moves then attacks (only if not disabled) =====
            bool bAttackCancelled = IsPairedBodyPartDisabled(fb, pairB);
            if (!bAttackCancelled)
            {
                match.Board.SetOccupied(newPosA, true);
                var newPosB = MovementEngine.ResolveMovement(fb, pairB, posB, newPosA, match.Board);
                match.Board.SetOccupied(newPosA, false);
                fb.HexQ = newPosB.Q; fb.HexR = newPosB.R;
                int distB = newPosA.DistanceTo(newPosB);
                round.Log.Add($"{fb.DisplayName} moves to {newPosB} (dist={distB}).");
                AttackEngine.Resolve(fb, pairB, fa, pairA, distB, round);
            }
            else
            {
                round.Log.Add($"{fb.DisplayName}'s action cancelled — required body part disabled.");
            }

            round.Outcome = resultA.Landed
                ? (bAttackCancelled ? RoundOutcome.FighterAWins : RoundOutcome.BothHit)
                : RoundOutcome.FighterBWins;
        }
        else if (round.FighterBFaster)
        {
            // ===== FASTER: B moves then attacks =====
            match.Board.SetOccupied(posA, true);
            var newPosB = MovementEngine.ResolveMovement(fb, pairB, posB, posA, match.Board);
            match.Board.SetOccupied(posA, false);
            fb.HexQ = newPosB.Q; fb.HexR = newPosB.R;
            int distB = posA.DistanceTo(newPosB);
            round.Log.Add($"{fb.DisplayName} moves to {newPosB} (dist={distB}).");

            var resultB = AttackEngine.Resolve(fb, pairB, fa, pairA, distB, round);

            // ===== SLOWER: A moves then attacks (only if not disabled) =====
            bool aAttackCancelled = IsPairedBodyPartDisabled(fa, pairA);
            if (!aAttackCancelled)
            {
                match.Board.SetOccupied(newPosB, true);
                var newPosA = MovementEngine.ResolveMovement(fa, pairA, posA, newPosB, match.Board, chosenMoveA);
                match.Board.SetOccupied(newPosB, false);
                fa.HexQ = newPosA.Q; fa.HexR = newPosA.R;
                int distA = newPosA.DistanceTo(newPosB);
                round.Log.Add($"{fa.DisplayName} moves to {newPosA} (dist={distA}).");
                AttackEngine.Resolve(fa, pairA, fb, pairB, distA, round);
            }
            else
            {
                round.Log.Add($"{fa.DisplayName}'s action cancelled — required body part disabled.");
            }

            round.Outcome = resultB.Landed
                ? (aAttackCancelled ? RoundOutcome.FighterBWins : RoundOutcome.BothHit)
                : RoundOutcome.FighterAWins;
        }
        else
        {
            // ===== SPEED TIE: simultaneous — both move then both attack =====
            var newPosA = MovementEngine.ResolveMovement(fa, pairA, posA, posB, match.Board, chosenMoveA);
            var newPosB = MovementEngine.ResolveMovement(fb, pairB, posB, posA, match.Board);
            fa.HexQ = newPosA.Q; fa.HexR = newPosA.R;
            fb.HexQ = newPosB.Q; fb.HexR = newPosB.R;
            int dist = newPosA.DistanceTo(newPosB);
            round.Log.Add($"Simultaneous: {fa.DisplayName} at {newPosA}, {fb.DisplayName} at {newPosB} (dist={dist}).");

            var resultA = AttackEngine.Resolve(fa, pairA, fb, pairB, dist, round);
            var resultB = AttackEngine.Resolve(fb, pairB, fa, pairA, dist, round);

            round.FighterAMissed = !resultA.InRange;
            round.FighterBMissed = !resultB.InRange;

            if (!resultA.Landed && !resultB.Landed)
                round.Outcome = RoundOutcome.BothMissed;
            else if (resultA.Landed && !resultB.Landed)
                round.Outcome = RoundOutcome.FighterAWins;
            else if (!resultA.Landed && resultB.Landed)
                round.Outcome = RoundOutcome.FighterBWins;
            else
                round.Outcome = RoundOutcome.BothHit;
        }

        // ===== COOLDOWNS =====
        ApplyCooldowns(fa, pairA, fb.StaggerTurnsRemaining > 0);
        ApplyCooldowns(fb, pairB, fa.StaggerTurnsRemaining > 0);
        fa.TickCooldowns();
        fb.TickCooldowns();

        // ===== KO CHECK =====
        bool aKO = fa.IsKnockedOut();
        bool bKO = fb.IsKnockedOut();

        if (aKO || bKO)
        {
            match.Phase = MatchPhase.MatchOver;
            if (aKO && bKO)
            {
                round.Log.Add("Double KO! Match is a draw.");
            }
            else if (aKO)
            {
                match.Winner = fb;
                match.Loser = fa;
                round.Log.Add($"{fb.DisplayName} wins by KO!");
            }
            else
            {
                match.Winner = fa;
                match.Loser = fb;
                round.Log.Add($"{fa.DisplayName} wins by KO!");
            }
        }
        else
        {
            match.Phase = MatchPhase.RoundResult;
        }

        match.CurrentRound++;
        match.History.Add(round);
        match.CurrentRoundState = round;

        return round;
    }

    private static bool IsPairedBodyPartDisabled(FighterInstance fighter, CardPair pair)
    {
        if (pair.Generic == null) return false;
        var loc = FighterInstance.BodyPartToLocation(pair.Generic.BodyPart);
        return fighter.LocationStates[loc].State == Models.Fighter.DamageState.Disabled;
    }

    private static void ApplyCooldowns(FighterInstance fighter, CardPair pair, bool staggered)
    {
        int extraStagger = staggered ? 1 : 0;

        if (pair.Generic != null)
        {
            int cd = fighter.GetCardCooldown(pair.Generic) + extraStagger;
            fighter.SetCooldown(pair.Generic.Id, cd);
        }
        if (pair.Unique != null)
        {
            int cd = fighter.GetCardCooldown(pair.Unique) + extraStagger;
            fighter.SetCooldown(pair.Unique.Id, cd);
        }
        if (pair.Special != null)
        {
            int cd = fighter.GetCardCooldown(pair.Special) + extraStagger;
            fighter.SetCooldown(pair.Special.Id, cd);
        }
    }
}
