using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;

namespace Grants.Engine;

/// <summary>
/// Orchestrates a full round of combat:
/// 1. Receive both committed card pairs
/// 2. Determine speed order
/// 3. Faster fighter: move then attack → pause (RoundMidpoint)
/// 4. Slower fighter: move then attack (can be cancelled if disabled by step 3)
/// 5. Speed tie: simultaneous move+attack → no midpoint pause
/// 6. Process keywords, cooldowns
/// 7. Check KO
/// Returns a RoundState (possibly partial — full state after ResolveSecondHalf).
/// </summary>
public static class ResolutionEngine
{
    /// <summary>
    /// Resolves the first fighter's action (move + attack).
    /// Sets match.Phase = RoundMidpoint and returns the partial RoundState.
    /// Call ResolveSecondHalf() after the player acknowledges.
    ///
    /// For speed ties, resolves the full round immediately (no midpoint pause).
    /// </summary>
    public static RoundState ResolveFirstHalf(MatchState match)
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
            SpeedA = speedA + fa.RoundSpeedModifier,
            SpeedB = speedB + fb.RoundSpeedModifier,
        };

        var posA = new HexCoord(fa.HexQ, fa.HexR);
        var posB = new HexCoord(fb.HexQ, fb.HexR);
        var chosenMoveA = match.ChosenMoveA;

        round.Log.Add($"--- Round {round.RoundNumber} ---");
        round.Log.Add($"Speed: {fa.DisplayName}={speedA}, {fb.DisplayName}={speedB}.");

        match.CurrentRoundState = round;

        // --- Clear round-scoped state before hooks populate it ---
        fa.ActiveImmunities.Clear();
        fb.ActiveImmunities.Clear();

        // --- Stage + Persona hooks: round start ---
        match.Stage.OnRoundStart(match, match.StageState);
        fa.Definition.Persona.OnRoundResolutionStart(round, match, fa, fb, fa.PersonaState);
        fb.Definition.Persona.OnRoundResolutionStart(round, match, fb, fa, fb.PersonaState);

        if (round.FighterAFaster)
        {
            // ===== FIRST: A moves then attacks =====
            match.Board.SetOccupied(posB, true);
            var newPosA = MovementEngine.ResolveMovement(fa, pairA, posA, posB, match.Board, chosenMoveA);
            match.Board.SetOccupied(posB, false);
            fa.HexQ = newPosA.Q; fa.HexR = newPosA.R;
            int distA = newPosA.DistanceTo(posB);
            round.Log.Add($"{fa.DisplayName} moves to {newPosA} (dist={distA}).");
            var resultA1 = AttackEngine.Resolve(fa, pairA, fb, pairB, distA, round);
            ApplyKnockback(resultA1, fa, fb, match.Board, round);
            ApplyPull(resultA1, fa, fb, match.Board, round);
            if (resultA1.Landed)
            {
                fa.Definition.Persona.OnLandedHit(fa, fb, pairA, resultA1, round, match, fa.PersonaState);
                fb.Definition.Persona.OnLandedHit(fa, fb, pairA, resultA1, round, match, fb.PersonaState);
            }
            round.FirstHalfLogCount = round.Log.Count;

            // Pause before second fighter
            match.Phase = MatchPhase.RoundMidpoint;
        }
        else if (round.FighterBFaster)
        {
            // ===== FIRST: B moves then attacks =====
            match.Board.SetOccupied(posA, true);
            var newPosB = MovementEngine.ResolveMovement(fb, pairB, posB, posA, match.Board);
            match.Board.SetOccupied(posA, false);
            fb.HexQ = newPosB.Q; fb.HexR = newPosB.R;
            int distB = posA.DistanceTo(newPosB);
            round.Log.Add($"{fb.DisplayName} moves to {newPosB} (dist={distB}).");
            var resultB1 = AttackEngine.Resolve(fb, pairB, fa, pairA, distB, round);
            ApplyKnockback(resultB1, fb, fa, match.Board, round);
            ApplyPull(resultB1, fb, fa, match.Board, round);
            if (resultB1.Landed)
            {
                fb.Definition.Persona.OnLandedHit(fb, fa, pairB, resultB1, round, match, fb.PersonaState);
                fa.Definition.Persona.OnLandedHit(fb, fa, pairB, resultB1, round, match, fa.PersonaState);
            }
            round.FirstHalfLogCount = round.Log.Count;

            // Pause before second fighter
            match.Phase = MatchPhase.RoundMidpoint;
        }
        else
        {
            // ===== SPEED TIE: resolve fully now (no midpoint) =====
            var newPosA = MovementEngine.ResolveMovement(fa, pairA, posA, posB, match.Board, chosenMoveA);
            var newPosB = MovementEngine.ResolveMovement(fb, pairB, posB, posA, match.Board);
            fa.HexQ = newPosA.Q; fa.HexR = newPosA.R;
            fb.HexQ = newPosB.Q; fb.HexR = newPosB.R;
            int dist = newPosA.DistanceTo(newPosB);
            round.Log.Add($"Simultaneous: {fa.DisplayName} at {newPosA}, {fb.DisplayName} at {newPosB} (dist={dist}).");

            var resultA = AttackEngine.Resolve(fa, pairA, fb, pairB, dist, round);
            ApplyKnockback(resultA, fa, fb, match.Board, round);
            ApplyPull(resultA, fa, fb, match.Board, round);
            if (resultA.Landed)
            {
                fa.Definition.Persona.OnLandedHit(fa, fb, pairA, resultA, round, match, fa.PersonaState);
                fb.Definition.Persona.OnLandedHit(fa, fb, pairA, resultA, round, match, fb.PersonaState);
            }
            var resultB = AttackEngine.Resolve(fb, pairB, fa, pairA, dist, round);
            ApplyKnockback(resultB, fb, fa, match.Board, round);
            ApplyPull(resultB, fb, fa, match.Board, round);
            if (resultB.Landed)
            {
                fb.Definition.Persona.OnLandedHit(fb, fa, pairB, resultB, round, match, fb.PersonaState);
                fa.Definition.Persona.OnLandedHit(fb, fa, pairB, resultB, round, match, fa.PersonaState);
            }

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

            // Disruption: if a fighter's generic card location took damage, apply +1 cooldown
            bool aTieDamaged = WasPairedLocationDamaged(fa, pairA, round.DamageToA);
            bool bTieDamaged = WasPairedLocationDamaged(fb, pairB, round.DamageToB);
            if (aTieDamaged)
                round.Log.Add($"  {fa.DisplayName}'s {pairA.Generic!.Name} disrupted by damage this round! (+1 cooldown)");
            if (bTieDamaged)
                round.Log.Add($"  {fb.DisplayName}'s {pairB.Generic!.Name} disrupted by damage this round! (+1 cooldown)");
            FinishRound(match, round, aTieDamaged, bTieDamaged);
        }

        return round;
    }

    /// <summary>
    /// Resolves the second fighter's action after the mid-round pause.
    /// Only valid when match.Phase == RoundMidpoint.
    /// </summary>
    public static void ResolveSecondHalf(MatchState match)
    {
        var round = match.CurrentRoundState!;
        var pairA = match.SelectedPairA!;
        var pairB = match.SelectedPairB!;
        var fa = match.FighterA;
        var fb = match.FighterB;

        var posA = new HexCoord(fa.HexQ, fa.HexR);
        var posB = new HexCoord(fb.HexQ, fb.HexR);

        if (round.FighterAFaster)
        {
            // A already acted; now B acts
            bool bLocDamaged = WasPairedLocationDamaged(fb, pairB, round.DamageToB);
            bool bCancelled = IsPairedBodyPartDisabled(fb, pairB) || bLocDamaged;
            if (!bCancelled)
            {
                match.Board.SetOccupied(posA, true);
                var newPosB = MovementEngine.ResolveMovement(fb, pairB, posB, posA, match.Board);
                match.Board.SetOccupied(posA, false);
                fb.HexQ = newPosB.Q; fb.HexR = newPosB.R;
                int distB = posA.DistanceTo(newPosB);
                round.Log.Add($"{fb.DisplayName} moves to {newPosB} (dist={distB}).");
                var resultB2 = AttackEngine.Resolve(fb, pairB, fa, pairA, distB, round);
                ApplyKnockback(resultB2, fb, fa, match.Board, round);
                ApplyPull(resultB2, fb, fa, match.Board, round);
                if (resultB2.Landed)
                {
                    fb.Definition.Persona.OnLandedHit(fb, fa, pairB, resultB2, round, match, fb.PersonaState);
                    fa.Definition.Persona.OnLandedHit(fb, fa, pairB, resultB2, round, match, fa.PersonaState);
                }
            }
            else
            {
                if (IsPairedBodyPartDisabled(fb, pairB))
                    round.Log.Add($"{fb.DisplayName}'s action cancelled -- required body part disabled.");
                else
                    round.Log.Add($"{fb.DisplayName}'s action disrupted -- location took damage this round! (+1 cooldown)");
            }

            // Determine outcome based on first-half attack result
            round.Outcome = bCancelled
                ? RoundOutcome.FighterAWins
                : RoundOutcome.BothHit; // refined below
            FinishRound(match, round, false, bLocDamaged);
        }
        else // FighterBFaster
        {
            // B already acted; now A acts
            bool aLocDamaged = WasPairedLocationDamaged(fa, pairA, round.DamageToA);
            bool aCancelled = IsPairedBodyPartDisabled(fa, pairA) || aLocDamaged;
            if (!aCancelled)
            {
                match.Board.SetOccupied(posB, true);
                var newPosA = MovementEngine.ResolveMovement(fa, pairA, posA, posB, match.Board, match.ChosenMoveA);
                match.Board.SetOccupied(posB, false);
                fa.HexQ = newPosA.Q; fa.HexR = newPosA.R;
                int distA = newPosA.DistanceTo(posB);
                round.Log.Add($"{fa.DisplayName} moves to {newPosA} (dist={distA}).");
                var resultA2 = AttackEngine.Resolve(fa, pairA, fb, pairB, distA, round);
                ApplyKnockback(resultA2, fa, fb, match.Board, round);
                ApplyPull(resultA2, fa, fb, match.Board, round);
                if (resultA2.Landed)
                {
                    fa.Definition.Persona.OnLandedHit(fa, fb, pairA, resultA2, round, match, fa.PersonaState);
                    fb.Definition.Persona.OnLandedHit(fa, fb, pairA, resultA2, round, match, fb.PersonaState);
                }
            }
            else
            {
                if (IsPairedBodyPartDisabled(fa, pairA))
                    round.Log.Add($"{fa.DisplayName}'s action cancelled -- required body part disabled.");
                else
                    round.Log.Add($"{fa.DisplayName}'s action disrupted -- location took damage this round! (+1 cooldown)");
            }

            round.Outcome = aCancelled
                ? RoundOutcome.FighterBWins
                : RoundOutcome.BothHit;
            FinishRound(match, round, aLocDamaged, false);
        }
    }

    private static void FinishRound(MatchState match, RoundState round, bool aGenericHit = false, bool bGenericHit = false)
    {
        var fa = match.FighterA;
        var fb = match.FighterB;

        // ===== COOLDOWNS =====
        ApplyCooldowns(fa, match.SelectedPairA!, fb.StaggerTurnsRemaining > 0, aGenericHit);
        ApplyCooldowns(fb, match.SelectedPairB!, fa.StaggerTurnsRemaining > 0, bGenericHit);

        // ===== KO CHECK =====
        bool aKO = fa.IsKnockedOut();
        bool bKO = fb.IsKnockedOut();

        if (aKO || bKO)
        {
            match.Phase = MatchPhase.MatchOver;
            if (aKO && bKO)
            {
                match.IsDraw = true;
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
            // ===== STALEMATE CHECK =====
            bool anyDamage = round.DamageToA.Count > 0 || round.DamageToB.Count > 0;
            if (anyDamage)
                match.ConsecutiveNoDamageRounds = 0;
            else
                match.ConsecutiveNoDamageRounds++;

            if (match.ConsecutiveNoDamageRounds >= 5)
            {
                match.Phase = MatchPhase.MatchOver;
                match.IsDraw = true;
                round.Log.Add("Draw! No damage dealt in 5 consecutive rounds.");
            }
            else
            {
                match.Phase = MatchPhase.RoundResult;
            }
        }

        // --- Stage + Persona hooks: round complete ---
        fa.Definition.Persona.OnRoundResolutionComplete(round, match, fa, fb, fa.PersonaState);
        fb.Definition.Persona.OnRoundResolutionComplete(round, match, fb, fa, fb.PersonaState);
        match.Stage.OnRoundComplete(round, match, match.StageState);

        match.CurrentRound++;
        match.History.Add(round);
        match.CurrentRoundState = round;
        match.ChosenMoveA = null;
    }

    private static bool IsPairedBodyPartDisabled(FighterInstance fighter, CardPair pair)
    {
        if (pair.Generic == null) return false;
        var loc = FighterInstance.BodyPartToLocation(pair.Generic.BodyPart);
        return fighter.LocationStates[loc].State == Models.Fighter.DamageState.Disabled;
    }

    private static bool WasPairedLocationDamaged(FighterInstance fighter, CardPair pair, Dictionary<BodyLocation, int> damageTaken)
    {
        if (pair.Generic == null) return false;
        var loc = FighterInstance.BodyPartToLocation(pair.Generic.BodyPart);
        return damageTaken.TryGetValue(loc, out int dmg) && dmg > 0;
    }

    private static void ApplyCooldowns(FighterInstance fighter, CardPair pair, bool staggered, bool locationHit = false)
    {
        int extraStagger = staggered ? 1 : 0;
        int extraDamage = locationHit ? 1 : 0;

        if (pair.Generic != null)
        {
            int cd = fighter.GetCardCooldown(pair.Generic) + extraStagger + extraDamage;
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

    /// <summary>
    /// If the attack result has CursePull triggered, pull the defender toward the attacker
    /// by the defender's curse token count, stopping when adjacent.
    /// </summary>
    private static void ApplyPull(
        AttackEngine.AttackResult result,
        FighterInstance attacker,
        FighterInstance defender,
        HexBoard board,
        RoundState round)
    {
        if (!result.Landed) return;

        // CursePull: pull by curse token count
        if (result.TriggeredKeywords.ContainsKeyword(CardKeyword.CursePull))
        {
            int steps = defender.PersonaState.Counters.GetValueOrDefault("curse_tokens", 0);
            if (steps > 0)
                PullDefender(attacker, defender, board, round, steps);
        }

        // Pull: pull by 1 hex
        if (result.TriggeredKeywords.ContainsKeyword(CardKeyword.Pull))
            PullDefender(attacker, defender, board, round, 1);
    }

    private static void PullDefender(
        FighterInstance attacker,
        FighterInstance defender,
        HexBoard board,
        RoundState round,
        int steps)
    {
        var defPos = new HexCoord(defender.HexQ, defender.HexR);
        var atkPos = new HexCoord(attacker.HexQ, attacker.HexR);

        for (int i = 0; i < steps; i++)
        {
            if (defPos.DistanceTo(atkPos) <= 1) break;
            var closest = defPos.GetNeighbors()
                .Where(h => board.IsValid(h) && !board.IsOccupied(h))
                .OrderBy(h => h.DistanceTo(atkPos))
                .FirstOrDefault();
            if (closest == default) break;
            defPos = closest;
        }

        if (defPos.Q != defender.HexQ || defPos.R != defender.HexR)
        {
            defender.HexQ = defPos.Q;
            defender.HexR = defPos.R;
            round.Log.Add($"  {defender.DisplayName} is pulled toward {attacker.DisplayName}! (Now at {defPos})");
        }
    }

    /// <summary>
    /// away from the attacker. No-op if the destination is out-of-bounds or occupied.
    /// </summary>
    private static void ApplyKnockback(
        AttackEngine.AttackResult result,
        FighterInstance attacker,
        FighterInstance defender,
        HexBoard board,
        RoundState round)
    {
        if (!result.TriggeredKeywords.ContainsKeyword(CardKeyword.Knockback)) return;

        var attackerPos = new HexCoord(attacker.HexQ, attacker.HexR);
        var defenderPos = new HexCoord(defender.HexQ, defender.HexR);
        int dirAway = HexMath.DirectionTo(attackerPos, defenderPos);
        var dest = defenderPos.Neighbor(dirAway);

        if (board.IsValid(dest) && !board.IsOccupied(dest))
        {
            defender.HexQ = dest.Q;
            defender.HexR = dest.R;
            round.Log.Add($"  {defender.DisplayName} is knocked back to {dest}.");
        }
        else
        {
            round.Log.Add($"  {defender.DisplayName} resists knockback (blocked).");
        }
    }
}
