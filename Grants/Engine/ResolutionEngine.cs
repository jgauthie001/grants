using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;

namespace Grants.Engine;

/// <summary>
/// Orchestrates a full round of combat:
/// 1. Receive both committed card pairs
/// 2. Determine speed order
/// 3. Apply movement (faster fighter first)
/// 4. Apply attacks in speed order
/// 5. Handle simultaneous (speed tie)
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

        round.Log.Add($"--- Round {round.RoundNumber} ---");
        round.Log.Add($"Speed: {fa.DisplayName}={speedA}, {fb.DisplayName}={speedB}.");

        // ===== MOVEMENT PHASE =====
        HexCoord newPosA, newPosB;

        if (round.FighterAFaster)
        {
            // A moves first — B hasn't moved yet, so B blocks A's movement
            match.Board.SetOccupied(posB, true);
            newPosA = MovementEngine.ResolveMovement(fa, pairA, posA, posB, match.Board);
            match.Board.SetOccupied(posB, false);
            match.Board.SetOccupied(newPosA, true);
            newPosB = MovementEngine.ResolveMovement(fb, pairB, posB, newPosA, match.Board);
            match.Board.SetOccupied(newPosA, false);
        }
        else if (round.FighterBFaster)
        {
            match.Board.SetOccupied(posA, true);
            newPosB = MovementEngine.ResolveMovement(fb, pairB, posB, posA, match.Board);
            match.Board.SetOccupied(posA, false);
            match.Board.SetOccupied(newPosB, true);
            newPosA = MovementEngine.ResolveMovement(fa, pairA, posA, newPosB, match.Board);
            match.Board.SetOccupied(newPosB, false);
        }
        else
        {
            // Speed tie: both move simultaneously from original positions
            newPosA = MovementEngine.ResolveMovement(fa, pairA, posA, posB, match.Board);
            newPosB = MovementEngine.ResolveMovement(fb, pairB, posB, posA, match.Board);
        }

        // Commit positions
        fa.HexQ = newPosA.Q; fa.HexR = newPosA.R;
        fb.HexQ = newPosB.Q; fb.HexR = newPosB.R;
        int distAfterMove = newPosA.DistanceTo(newPosB);

        round.Log.Add($"After movement: dist={distAfterMove}. {fa.DisplayName} at {newPosA}, {fb.DisplayName} at {newPosB}.");

        // ===== ATTACK PHASE =====
        if (round.FighterAFaster)
        {
            // A attacks from new positions; if A lands and disables B's paired body part, B's attack is cancelled
            var resultA = AttackEngine.Resolve(fa, pairA, fb, pairB, distAfterMove, round);
            round.FighterBMissed = !resultA.InRange; // B is the target — irrelevant but symmetric

            bool bAttackCancelled = IsPairedBodyPartDisabled(fb, pairB);
            if (!bAttackCancelled)
                AttackEngine.Resolve(fb, pairB, fa, pairA, distAfterMove, round);
            else
                round.Log.Add($"{fb.DisplayName}'s attack cancelled — required body part disabled mid-round.");

            round.Outcome = resultA.Landed
                ? (bAttackCancelled ? RoundOutcome.FighterAWins : RoundOutcome.BothHit)
                : RoundOutcome.FighterBWins;
        }
        else if (round.FighterBFaster)
        {
            var resultB = AttackEngine.Resolve(fb, pairB, fa, pairA, distAfterMove, round);
            bool aAttackCancelled = IsPairedBodyPartDisabled(fa, pairA);
            if (!aAttackCancelled)
                AttackEngine.Resolve(fa, pairA, fb, pairB, distAfterMove, round);
            else
                round.Log.Add($"{fa.DisplayName}'s attack cancelled — required body part disabled mid-round.");

            round.Outcome = resultB.Landed
                ? (aAttackCancelled ? RoundOutcome.FighterBWins : RoundOutcome.BothHit)
                : RoundOutcome.FighterAWins;
        }
        else
        {
            // Speed tie: simultaneous — both resolve, no cancellation, both take damage
            var resultA = AttackEngine.Resolve(fa, pairA, fb, pairB, distAfterMove, round);
            var resultB = AttackEngine.Resolve(fb, pairB, fa, pairA, distAfterMove, round);

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
