using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;

namespace Grants.Engine;

/// <summary>
/// Orchestrates a full round of combat using the structured phase system:
///   Start → Beginning (priority then second) → Main (priority then second) →
///   Final (priority then second) → End.
/// Cards declare which phase each action fires in via TurnPhase properties.
/// Returns a RoundState (possibly partial — full state after ResolveSecondHalf).
/// </summary>
public static class ResolutionEngine
{
    /// <summary>
    /// Resolves the first half of a round:
    ///   Beginning phase (both fighters) →
    ///   Main phase, priority only → Pause (RoundMidpoint).
    /// For speed ties, resolves all phases at once (no midpoint pause).
    /// </summary>
    public static RoundState ResolveFirstHalf(MatchState match)
    {
        var pairA = match.SelectedPairA!;
        var pairB = match.SelectedPairB!;
        var fa = match.FighterA;
        var fb = match.FighterB;

        // --- Clear round-scoped state and fire pre-round hooks BEFORE computing speed ---
        // Persona hooks like HonourDebtPersona modify RoundSpeedModifier here,
        // so they must run before SpeedA/SpeedB are baked into RoundState.
        fa.ActiveImmunities.Clear();
        fb.ActiveImmunities.Clear();

        // Temporary round stub so hooks have a log to write to
        var preRound = new RoundState
        {
            RoundNumber = match.CurrentRound,
            PairA = pairA,
            PairB = pairB,
            SpeedA = 0, SpeedB = 0,
        };
        match.Stage.OnRoundStart(match, match.StageState);
        fa.Definition.Persona.OnRoundResolutionStart(preRound, match, fa, fb, fa.PersonaState);
        fb.Definition.Persona.OnRoundResolutionStart(preRound, match, fb, fa, fb.PersonaState);

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
        round.Log.AddRange(preRound.Log);
        round.Log.Add($"--- Round {round.RoundNumber} ---");
        round.Log.Add($"Speed: {fa.DisplayName}={speedA}, {fb.DisplayName}={speedB}.");
        match.CurrentRoundState = round;

        if (round.SpeedTie)
        {
            // ===== SPEED TIE: resolve all phases simultaneously, no midpoint pause =====

            // === BEGINNING PHASE ===
            round.Log.Add("--- Beginning Phase ---");
            if (pairA.GenericMovementPhase == TurnPhase.Beginning)
                ExecuteGenericMove(fa, pairA, fb, match, round, match.ChosenMoveA);
            if (pairB.GenericMovementPhase == TurnPhase.Beginning)
                ExecuteGenericMove(fb, pairB, fa, match, round, null);

            // === MAIN PHASE (simultaneous) ===
            round.Log.Add("--- Main Phase ---");
            int dist = new HexCoord(fa.HexQ, fa.HexR).DistanceTo(new HexCoord(fb.HexQ, fb.HexR));
            round.Log.Add($"Simultaneous: {fa.DisplayName} at ({fa.HexQ},{fa.HexR}), {fb.DisplayName} at ({fb.HexQ},{fb.HexR}) (dist={dist}).");

            AttackEngine.AttackResult? resultA = null;
            AttackEngine.AttackResult? resultB = null;

            if (pairA.AttackPhase == TurnPhase.Main)
            {
                var rA = AttackEngine.Resolve(fa, pairA, fb, pairB, dist, round);
                ApplyKnockback(rA, fa, fb, match.Board, round);
                ApplyPull(rA, fa, fb, match.Board, round);
                if (rA.Landed)
                {
                    fa.Definition.Persona.OnLandedHit(fa, fb, pairA, rA, round, match, fa.PersonaState);
                    fb.Definition.Persona.OnLandedHit(fa, fb, pairA, rA, round, match, fb.PersonaState);
                }
                resultA = rA;
            }
            if (pairB.AttackPhase == TurnPhase.Main)
            {
                var rB = AttackEngine.Resolve(fb, pairB, fa, pairA, dist, round);
                ApplyKnockback(rB, fb, fa, match.Board, round);
                ApplyPull(rB, fb, fa, match.Board, round);
                if (rB.Landed)
                {
                    fb.Definition.Persona.OnLandedHit(fb, fa, pairB, rB, round, match, fb.PersonaState);
                    fa.Definition.Persona.OnLandedHit(fb, fa, pairB, rB, round, match, fa.PersonaState);
                }
                resultB = rB;
            }

            // === FINAL PHASE ===
            round.Log.Add("--- Final Phase ---");
            if (resultA.HasValue) ApplyAttackerPostMove(resultA.Value, fa, pairA, fb, match.Board, round);
            if (resultB.HasValue) ApplyAttackerPostMove(resultB.Value, fb, pairB, fa, match.Board, round);

            // === OUTCOME ===
            round.FighterAMissed = resultA.HasValue && !resultA.Value.InRange;
            round.FighterBMissed = resultB.HasValue && !resultB.Value.InRange;

            if (resultA?.Landed != true && resultB?.Landed != true)
                round.Outcome = RoundOutcome.BothMissed;
            else if (resultA?.Landed == true && resultB?.Landed != true)
                round.Outcome = RoundOutcome.FighterAWins;
            else if (resultA?.Landed != true && resultB?.Landed == true)
                round.Outcome = RoundOutcome.FighterBWins;
            else
                round.Outcome = RoundOutcome.BothHit;

            bool aTieDamaged = WasPairedLocationDamaged(fa, pairA, round.DamageToA);
            bool bTieDamaged = WasPairedLocationDamaged(fb, pairB, round.DamageToB);
            if (aTieDamaged)
                round.Log.Add($"  {fa.DisplayName}'s {pairA.Generic!.Name} disrupted by damage this round! (+1 cooldown)");
            if (bTieDamaged)
                round.Log.Add($"  {fb.DisplayName}'s {pairB.Generic!.Name} disrupted by damage this round! (+1 cooldown)");
            FinishRound(match, round, aTieDamaged, bTieDamaged);
            return round;
        }

        // Non-tie: determine priority and second fighters
        bool aIsPriority  = round.FighterAFaster;
        FighterInstance priority = aIsPriority ? fa : fb;
        FighterInstance second   = aIsPriority ? fb : fa;
        CardPair priorityPair    = aIsPriority ? pairA : pairB;
        CardPair secondPair      = aIsPriority ? pairB : pairA;
        // FighterA (human) always uses ChosenMoveA; FighterB (AI) always auto-resolves
        HexCoord? priorityChosenMove = aIsPriority ? match.ChosenMoveA : null;
        HexCoord? secondChosenMove   = aIsPriority ? null : match.ChosenMoveA;

        // === BEGINNING PHASE — both fighters ===
        round.Log.Add("--- Beginning Phase ---");
        round.PriorityAttackResult = ExecuteFighterPhaseActions(
            priority, priorityPair, second, secondPair,
            TurnPhase.Beginning, match, round, priorityChosenMove, null);
        round.SecondAttackResult = ExecuteFighterPhaseActions(
            second, secondPair, priority, priorityPair,
            TurnPhase.Beginning, match, round, secondChosenMove, null);

        // === MAIN PHASE — priority only (second resolves in ResolveSecondHalf) ===
        round.Log.Add("--- Main Phase ---");
        round.PriorityAttackResult = ExecuteFighterPhaseActions(
            priority, priorityPair, second, secondPair,
            TurnPhase.Main, match, round, null, round.PriorityAttackResult);

        round.FirstHalfLogCount = round.Log.Count;
        match.Phase = MatchPhase.RoundMidpoint;
        return round;
    }

    /// <summary>
    /// Resolves the second half of a round after the mid-round pause:
    ///   Main phase (second fighter) → Final phase (priority then second) → End phase.
    /// Only valid when match.Phase == RoundMidpoint.
    /// </summary>
    public static void ResolveSecondHalf(MatchState match)
    {
        var round  = match.CurrentRoundState!;
        var pairA  = match.SelectedPairA!;
        var pairB  = match.SelectedPairB!;
        var fa     = match.FighterA;
        var fb     = match.FighterB;

        bool aIsPriority  = round.FighterAFaster;
        FighterInstance priority = aIsPriority ? fa : fb;
        FighterInstance second   = aIsPriority ? fb : fa;
        CardPair priorityPair    = aIsPriority ? pairA : pairB;
        CardPair secondPair      = aIsPriority ? pairB : pairA;

        // === MAIN PHASE — SECOND ===
        // Second's action can be disrupted if priority's Main attack damaged
        // the body location associated with second's generic card.
        var secondDamageTaken = aIsPriority ? round.DamageToB : round.DamageToA;
        bool secondGenericHit = WasPairedLocationDamaged(second, secondPair, secondDamageTaken);
        bool secondCancelled  = IsPairedBodyPartDisabled(second, secondPair) || secondGenericHit;

        if (!secondCancelled)
        {
            round.SecondAttackResult = ExecuteFighterPhaseActions(
                second, secondPair, priority, priorityPair,
                TurnPhase.Main, match, round, null, round.SecondAttackResult);
        }
        else
        {
            if (IsPairedBodyPartDisabled(second, secondPair))
                round.Log.Add($"{second.DisplayName}'s action cancelled -- required body part disabled.");
            else
                round.Log.Add($"{second.DisplayName}'s action disrupted -- location took damage this round! (+1 cooldown)");
        }

        // === FINAL PHASE — priority then second ===
        round.Log.Add("--- Final Phase ---");
        ExecuteFighterPhaseActions(
            priority, priorityPair, second, secondPair,
            TurnPhase.Finish, match, round, null, round.PriorityAttackResult);
        if (!secondCancelled)
            ExecuteFighterPhaseActions(
                second, secondPair, priority, priorityPair,
                TurnPhase.Finish, match, round, null, round.SecondAttackResult);

        // === OUTCOME ===
        bool priorityLanded = round.PriorityAttackResult?.Landed == true;
        bool secondLanded   = !secondCancelled && round.SecondAttackResult?.Landed == true;

        if (priorityLanded && secondLanded)
            round.Outcome = RoundOutcome.BothHit;
        else if (priorityLanded)
            round.Outcome = aIsPriority ? RoundOutcome.FighterAWins : RoundOutcome.FighterBWins;
        else if (secondLanded)
            round.Outcome = aIsPriority ? RoundOutcome.FighterBWins : RoundOutcome.FighterAWins;
        else
            round.Outcome = secondCancelled
                ? (aIsPriority ? RoundOutcome.FighterAWins : RoundOutcome.FighterBWins)
                : RoundOutcome.BothMissed;

        round.FighterAMissed = aIsPriority
            ? (round.PriorityAttackResult.HasValue && !round.PriorityAttackResult.Value.InRange)
            : (!secondCancelled && round.SecondAttackResult.HasValue && !round.SecondAttackResult.Value.InRange);
        round.FighterBMissed = aIsPriority
            ? (!secondCancelled && round.SecondAttackResult.HasValue && !round.SecondAttackResult.Value.InRange)
            : (round.PriorityAttackResult.HasValue && !round.PriorityAttackResult.Value.InRange);

        bool aGenericHit = aIsPriority ? false : secondGenericHit;
        bool bGenericHit = aIsPriority ? secondGenericHit : false;
        FinishRound(match, round, aGenericHit, bGenericHit);
    }

    /// <summary>
    /// Executes all phase-appropriate actions for one fighter:
    ///   1. Generic movement (if declared for this phase)
    ///   2. Attack (if declared for this phase)
    ///   3. Post-attack repositioning (if declared for this phase)
    /// Returns the most recent attack result (new result if attack fired, otherwise cachedAttackResult).
    /// </summary>
    private static AttackEngine.AttackResult? ExecuteFighterPhaseActions(
        FighterInstance actor, CardPair actorPair,
        FighterInstance opponent, CardPair opponentPair,
        TurnPhase phase, MatchState match, RoundState round,
        HexCoord? chosenMove,
        AttackEngine.AttackResult? cachedAttackResult)
    {
        // 1. Generic card movement
        if (actorPair.GenericMovementPhase == phase)
            ExecuteGenericMove(actor, actorPair, opponent, match, round, chosenMove);

        // 2. Attack
        AttackEngine.AttackResult? newResult = null;
        if (actorPair.AttackPhase == phase)
        {
            int dist = new HexCoord(actor.HexQ, actor.HexR)
                           .DistanceTo(new HexCoord(opponent.HexQ, opponent.HexR));
            var r = AttackEngine.Resolve(actor, actorPair, opponent, opponentPair, dist, round);
            ApplyKnockback(r, actor, opponent, match.Board, round);
            ApplyPull(r, actor, opponent, match.Board, round);
            if (r.Landed)
            {
                actor.Definition.Persona.OnLandedHit(actor, opponent, actorPair, r, round, match, actor.PersonaState);
                opponent.Definition.Persona.OnLandedHit(actor, opponent, actorPair, r, round, match, opponent.PersonaState);
            }
            newResult = r;
        }

        // 3. Post-attack repositioning (uses the freshest known attack result)
        var effectiveResult = newResult ?? cachedAttackResult;
        if (actorPair.PostMovementPhase == phase && effectiveResult.HasValue)
            ApplyAttackerPostMove(effectiveResult.Value, actor, actorPair, opponent, match.Board, round);

        return newResult ?? cachedAttackResult;
    }

    /// <summary>
    /// Executes the generic card's movement. Caller must verify GenericMovementPhase matches
    /// the current phase before calling.
    /// </summary>
    private static void ExecuteGenericMove(
        FighterInstance mover,
        CardPair pair,
        FighterInstance opponent,
        MatchState match,
        RoundState round,
        HexCoord? chosenDest)
    {
        int maxMovement = pair.Generic != null ? mover.GetCardMovement(pair.Generic) : 0;
        if (maxMovement <= 0 || pair.CombinedMovementType == MovementType.None) return;

        var moverPos = new HexCoord(mover.HexQ, mover.HexR);
        var oppPos   = new HexCoord(opponent.HexQ, opponent.HexR);
        match.Board.SetOccupied(oppPos, true);
        var newPos = MovementEngine.ResolveMovement(mover, pair, moverPos, oppPos, match.Board, chosenDest);
        match.Board.SetOccupied(oppPos, false);
        mover.HexQ = newPos.Q; mover.HexR = newPos.R;
        int dist = newPos.DistanceTo(oppPos);
        round.Log.Add($"{mover.DisplayName} moves to {newPos} (dist={dist}).");
    }

    /// <summary>
    /// Applies post-attack repositioning for the attacker after their attack resolves.
    /// Handles: unique/special card post-movement, plus Recoil/FollowThrough/Disengage keywords.
    /// Post-movement is always auto-resolved — no player destination choice.
    /// </summary>
    private static void ApplyAttackerPostMove(
        AttackEngine.AttackResult result,
        FighterInstance attacker,
        CardPair attackerPair,
        FighterInstance defender,
        HexBoard board,
        RoundState round)
    {
        var attackerPos = new HexCoord(attacker.HexQ, attacker.HexR);
        var defenderPos = new HexCoord(defender.HexQ, defender.HexR);

        // 1. Card-field post-movement (unique/special card owns the post-attack phase)
        if (attackerPair.PostMovementMax > 0 && attackerPair.PostMovementType != MovementType.None)
        {
            board.SetOccupied(defenderPos, true);
            var newPos = MovementEngine.ResolvePostMovement(attacker, attackerPair, attackerPos, defenderPos, board);
            board.SetOccupied(defenderPos, false);
            if (newPos != attackerPos)
            {
                attacker.HexQ = newPos.Q; attacker.HexR = newPos.R;
                round.Log.Add($"  {attacker.DisplayName} repositions after attack (to {newPos}).");
                attackerPos = newPos;
                defenderPos = new HexCoord(defender.HexQ, defender.HexR); // refresh in case defender moved
            }
        }

        // Collect keywords for keyword-driven post-move effects
        var atkKeywords = new List<CardKeywordValue>();
        if (attackerPair.Generic != null) atkKeywords.AddRange(attacker.GetCardKeywords(attackerPair.Generic));
        if (attackerPair.Unique  != null) atkKeywords.AddRange(attacker.GetCardKeywords(attackerPair.Unique));
        if (attackerPair.Special != null) atkKeywords.AddRange(attacker.GetCardKeywords(attackerPair.Special));

        // 2. Recoil: retreat N hexes after attack regardless of outcome
        if (atkKeywords.ContainsKeyword(CardKeyword.Recoil))
        {
            int n = atkKeywords.GetKeywordValue(CardKeyword.Recoil);
            defenderPos = new HexCoord(defender.HexQ, defender.HexR);
            board.SetOccupied(defenderPos, true);
            var recoilPos = MovementEngine.ApplyDirectionalMove(n, MovementType.Retreat, attackerPos, defenderPos, board);
            board.SetOccupied(defenderPos, false);
            if (recoilPos != attackerPos)
            {
                attacker.HexQ = recoilPos.Q; attacker.HexR = recoilPos.R;
                round.Log.Add($"  {attacker.DisplayName} recoils {n} hex(es) back!");
                attackerPos = recoilPos;
            }
        }

        // 3. FollowThrough: advance N toward opponent, but only on a landed hit
        if (result.Landed && atkKeywords.ContainsKeyword(CardKeyword.FollowThrough))
        {
            int n = atkKeywords.GetKeywordValue(CardKeyword.FollowThrough);
            defenderPos = new HexCoord(defender.HexQ, defender.HexR);
            board.SetOccupied(defenderPos, true);
            var followPos = MovementEngine.ApplyDirectionalMove(n, MovementType.Approach, attackerPos, defenderPos, board);
            board.SetOccupied(defenderPos, false);
            if (followPos != attackerPos)
            {
                attacker.HexQ = followPos.Q; attacker.HexR = followPos.R;
                round.Log.Add($"  {attacker.DisplayName} follows through! (to {followPos})");
                attackerPos = followPos;
            }
        }

        // 4. Disengage: retreat N when attack was out of range (couldn't land)
        if (!result.InRange && atkKeywords.ContainsKeyword(CardKeyword.Disengage))
        {
            int n = atkKeywords.GetKeywordValue(CardKeyword.Disengage);
            defenderPos = new HexCoord(defender.HexQ, defender.HexR);
            board.SetOccupied(defenderPos, true);
            var disengagePos = MovementEngine.ApplyDirectionalMove(n, MovementType.Retreat, attackerPos, defenderPos, board);
            board.SetOccupied(defenderPos, false);
            if (disengagePos != attackerPos)
            {
                attacker.HexQ = disengagePos.Q; attacker.HexR = disengagePos.R;
                round.Log.Add($"  {attacker.DisplayName} disengages safely.");
            }
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
