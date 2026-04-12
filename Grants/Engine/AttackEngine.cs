using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;

namespace Grants.Engine;

/// <summary>
/// Resolves the attack portion of a round: range check, power vs defense,
/// damage application to the correct body location, and keyword processing.
/// </summary>
public static class AttackEngine
{
    public struct AttackResult
    {
        public bool InRange;
        public bool Landed;
        public int PowerFinal;
        public int DefenseFinal;
        public int NetDamageSteps;
        public BodyLocation TargetLocation;
        public List<CardKeywordValue> TriggeredKeywords;
    }

    /// <summary>
    /// Resolve one fighter's attack against the other.
    /// Returns the result (whether it landed, damage, keywords triggered).
    /// </summary>
    public static AttackResult Resolve(
        FighterInstance attacker,
        CardPair attackerPair,
        FighterInstance defender,
        CardPair defenderPair,
        int currentDistance,
        RoundState round)
    {
        var result = new AttackResult
        {
            TriggeredKeywords = new List<CardKeywordValue>(),
        };

        // Build keyword lists via FighterInstance so upgrade-added keywords are included
        var atkKeywords = new List<CardKeywordValue>();
        if (attackerPair.Generic != null) atkKeywords.AddRange(attacker.GetCardKeywords(attackerPair.Generic));
        if (attackerPair.Unique  != null) atkKeywords.AddRange(attacker.GetCardKeywords(attackerPair.Unique));
        if (attackerPair.Special != null) atkKeywords.AddRange(attacker.GetCardKeywords(attackerPair.Special));

        var defKeywords = new List<CardKeywordValue>();
        if (defenderPair.Generic != null) defKeywords.AddRange(defender.GetCardKeywords(defenderPair.Generic));
        if (defenderPair.Unique  != null) defKeywords.AddRange(defender.GetCardKeywords(defenderPair.Unique));
        if (defenderPair.Special != null) defKeywords.AddRange(defender.GetCardKeywords(defenderPair.Special));

        // --- Range check ---
        // Attacks hit if distance is within the min/max range bracket
        // Keywords like Lunge can extend the maximum range
        int minRequiredRange = attackerPair.EffectiveMinRange;
        int maxRequiredRange = attackerPair.EffectiveMaxRange;

        // Lunge keyword: +1 to maximum range
        if (atkKeywords.ContainsKeyword(CardKeyword.Lunge))
            maxRequiredRange++;

        // Upgraded range extension bonus from upgrade tree
        if (attackerPair.Generic != null)
            maxRequiredRange += attacker.UpgradedCardMaxRange.GetValueOrDefault(attackerPair.Generic.Id, 0);

        result.InRange = currentDistance >= minRequiredRange && currentDistance <= maxRequiredRange;
        if (!result.InRange)
        {
            round.Log.Add($"{attacker.DisplayName} is out of range (dist={currentDistance}, range={minRequiredRange}-{maxRequiredRange}).");
            return result;
        }

        // --- Determine target location ---
        // Read primary/secondary targets from the attacker's unique or special card.
        // If the primary location on the defender is already Disabled, hit the secondary instead.
        BodyLocation primaryTarget = attackerPair.Unique?.PrimaryTarget
            ?? attackerPair.Special?.PrimaryTarget
            ?? BodyLocation.Torso;
        BodyLocation secondaryTarget = attackerPair.Unique?.SecondaryTarget
            ?? attackerPair.Special?.SecondaryTarget
            ?? primaryTarget;

        bool primaryDisabled = defender.LocationStates[primaryTarget].State == DamageState.Disabled;
        result.TargetLocation = primaryDisabled ? secondaryTarget : primaryTarget;

        if (primaryDisabled && primaryTarget != secondaryTarget)
            round.Log.Add($"  ({primaryTarget} disabled -- redirecting to {secondaryTarget})");

        // --- Power vs Defense ---
        int attackerPower = attacker.GetCardPower(attackerPair.Generic ?? (CardBase)attackerPair.Special!)
                          + attacker.GetCardPower(attackerPair.Unique ?? (CardBase)attackerPair.Special!)
                          + attacker.RoundPowerModifier;

        int defenderDefense = defender.GetCardDefense(defenderPair.Generic ?? (CardBase)defenderPair.Special!)
                            + defender.GetCardDefense(defenderPair.Unique ?? (CardBase)defenderPair.Special!);

        // --- Keyword modifiers ---
        // ArmorBreak: reduce defender defense by 1
        if (atkKeywords.ContainsKeyword(CardKeyword.ArmorBreak))
        {
            if (!defender.ActiveImmunities.Contains(CombatImmunity.DefenseReduction))
            {
                defenderDefense = Math.Max(0, defenderDefense - 1);
                result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.ArmorBreak));
            }
        }

        // Piercing: ignore half defender defense
        if (atkKeywords.ContainsKeyword(CardKeyword.Piercing))
        {
            if (!defender.ActiveImmunities.Contains(CombatImmunity.DefenseReduction))
            {
                defenderDefense = defenderDefense / 2;
                result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.Piercing));
            }
        }

        // Guard keyword on defender: +2 defense
        if (defKeywords.ContainsKeyword(CardKeyword.Guard))
        {
            defenderDefense += 2;
            result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.Guard));
        }

        // DistanceGuard: if distance >= threshold, defender gains +2 defense
        if (defKeywords.ContainsKeyword(CardKeyword.DistanceGuard))
        {
            int threshold = defKeywords.GetKeywordValue(CardKeyword.DistanceGuard);
            if (currentDistance >= threshold)
            {
                defenderDefense += 2;
                result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.DistanceGuard, threshold));
            }
        }

        // CurseEmpower: +N power (N = owner's curse pool)
        if (atkKeywords.ContainsKeyword(CardKeyword.CurseEmpower))
        {
            int pool = attacker.PersonaState.Counters.GetValueOrDefault("cursed_pool", 0);
            if (pool > 0)
            {
                attackerPower += pool;
                result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.CurseEmpower, pool));
            }
        }

        // CurseWeaken: -N defense (N = defender's curse tokens)
        if (atkKeywords.ContainsKeyword(CardKeyword.CurseWeaken))
        {
            if (!defender.ActiveImmunities.Contains(CombatImmunity.DefenseReduction))
            {
                int tokens = defender.PersonaState.Counters.GetValueOrDefault("curse_tokens", 0);
                if (tokens > 0)
                {
                    defenderDefense = Math.Max(0, defenderDefense - tokens);
                    result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.CurseWeaken, tokens));
                }
            }
        }

        result.PowerFinal = attackerPower;
        result.DefenseFinal = defenderDefense;

        int net = attackerPower - defenderDefense;
        result.Landed = net > 0;

        if (!result.Landed)
        {
            round.Log.Add($"{attacker.DisplayName}'s attack is blocked (power={attackerPower}, defense={defenderDefense}).");
            return result;
        }

        // --- Calculate damage steps ---
        // Triangular cost: 1st step costs 1 net, 2nd costs 2, 3rd costs 3, etc.
        // e.g. net=1 → 1 step, net=3 → 2 steps, net=6 → 3 steps
        int stepsCalc = 0;
        int stepCost = 1;
        int remaining = net;
        while (remaining >= stepCost)
        {
            remaining -= stepCost;
            stepsCalc++;
            stepCost++;
        }
        result.NetDamageSteps = stepsCalc;

        // ChivalryBonus: +N damage steps if defender holds >=1 chivalry token
        if (atkKeywords.ContainsKeyword(CardKeyword.ChivalryBonus))
        {
            int tokens = defender.PersonaState.Counters.GetValueOrDefault("chivalry_tokens", 0);
            if (tokens > 0)
            {
                int bonus = atkKeywords.GetKeywordValue(CardKeyword.ChivalryBonus);
                result.NetDamageSteps += bonus;
                result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.ChivalryBonus, bonus));
            }
        }

        // DistanceGuard: if defending at range >= value, apply +2 defense (processed on defender's keywords)
        // Note: DistanceGuard is checked earlier in the defense calculation above; handled below in defKeywords.

        // Crushing keyword: +1 damage step
        if (atkKeywords.ContainsKeyword(CardKeyword.Crushing))
        {
            result.NetDamageSteps++;
            result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.Crushing));
        }

        // MaxDamageCap: defender's card caps how far a single hit can advance the target location
        if (defKeywords.ContainsKeyword(CardKeyword.MaxDamageCap))
        {
            int cap = defKeywords.GetKeywordValue(CardKeyword.MaxDamageCap);
            var locState = defender.LocationStates[result.TargetLocation];
            int maxSteps = Math.Max(0, cap - (int)locState.State);
            if (result.NetDamageSteps > maxSteps)
            {
                result.NetDamageSteps = maxSteps;
                result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.MaxDamageCap, cap));
                string capName = cap == 1 ? "Bruised" : cap == 2 ? "Injured" : $"level {cap}";
                if (maxSteps == 0)
                    round.Log.Add($"  {defender.DisplayName}'s damage cap absorbs the hit — location already at or past {capName}!");
                else
                    round.Log.Add($"  {defender.DisplayName}'s damage cap: limited to {maxSteps} step(s) (max: {capName}).");
            }
        }

        round.Log.Add(
            $"{attacker.DisplayName} hits {defender.DisplayName}'s {result.TargetLocation} " +
            $"for {result.NetDamageSteps} damage step(s). (power={attackerPower}, def={defenderDefense})");

        // --- Apply keywords that modify target ---
        if (atkKeywords.ContainsKeyword(CardKeyword.Bleed))
        {
            if (!defender.ActiveImmunities.Contains(CombatImmunity.Bleed))
            {
                int bleedVal = atkKeywords.GetKeywordValue(CardKeyword.Bleed);
                defender.LocationStates[result.TargetLocation].BleedStacks += bleedVal;
                result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.Bleed, bleedVal));
                round.Log.Add($"  {defender.DisplayName}'s {result.TargetLocation} is now bleeding!");
            }
        }

        if (atkKeywords.ContainsKeyword(CardKeyword.Stagger))
        {
            if (!defender.ActiveImmunities.Contains(CombatImmunity.Stagger))
            {
                defender.StaggerTurnsRemaining = 1;
                result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.Stagger));
                round.Log.Add($"  {defender.DisplayName} is staggered -- cooldowns +1 next turn.");
            }
        }

        if (atkKeywords.ContainsKeyword(CardKeyword.Knockback))
        {
            if (!defender.ActiveImmunities.Contains(CombatImmunity.Push))
                result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.Knockback));
            // Spatial movement is applied by ResolutionEngine after Resolve() returns
        }

        // Pull: pull opponent 1 hex toward attacker on hit
        if (atkKeywords.ContainsKeyword(CardKeyword.Pull))
        {
            if (!defender.ActiveImmunities.Contains(CombatImmunity.Pull))
                result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.Pull));
        }

        // CurseGain: extra pool token on hit (handled by OnLandedHit via triggered keyword)
        if (atkKeywords.ContainsKeyword(CardKeyword.CurseGain))
            result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.CurseGain));

        // CursePull: pull opponent by their curse token count (movement handled by ResolutionEngine)
        if (atkKeywords.ContainsKeyword(CardKeyword.CursePull))
        {
            if (!defender.ActiveImmunities.Contains(CombatImmunity.Pull))
                result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.CursePull));
        }

        // Kill keyword: instantly disable all body parts (TEST ONLY)
        if (atkKeywords.ContainsKeyword(CardKeyword.Kill))
        {
            foreach (var loc in defender.LocationStates.Values)
            {
                loc.State = DamageState.Disabled;
            }
            result.TriggeredKeywords.Add(new CardKeywordValue(CardKeyword.Kill));
            round.Log.Add($"  *** {defender.DisplayName} is DEFEATED by Kill keyword! ***");
        }

        // --- Apply damage ---
        defender.LocationStates[result.TargetLocation].ApplyDamage(result.NetDamageSteps);

        // Record last hit location and accumulate per-location damage for disruption checks
        if (ReferenceEquals(attackerPair, round.PairA))
        {
            round.LastHitOnB = result.TargetLocation;
            round.DamageToB.TryGetValue(result.TargetLocation, out int prevB);
            round.DamageToB[result.TargetLocation] = prevB + result.NetDamageSteps;
        }
        else
        {
            round.LastHitOnA = result.TargetLocation;
            round.DamageToA.TryGetValue(result.TargetLocation, out int prevA);
            round.DamageToA[result.TargetLocation] = prevA + result.NetDamageSteps;
        }

        return result;
    }

    /// <summary>Process bleed stacks on target fighter at start of their turn.</summary>
    public static void ProcessBleed(FighterInstance fighter, RoundState round)
    {
        foreach (var loc in fighter.LocationStates.Values)
        {
            if (loc.BleedStacks > 0)
            {
                loc.ApplyDamage(loc.BleedStacks);
                round.Log.Add($"{fighter.DisplayName}'s {loc.Location} bleeds for {loc.BleedStacks} step(s).");
                loc.BleedStacks = 0;
            }
        }
    }
}
