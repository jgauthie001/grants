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
        public List<CardKeyword> TriggeredKeywords;
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
            TriggeredKeywords = new List<CardKeyword>(),
        };

        // --- Range check ---
        int requiredRange = (int)attackerPair.EffectiveRange;
        // Lunge keyword: +1 range
        if (attackerPair.AllKeywords.ContainsKeyword(CardKeyword.Lunge))
            requiredRange++;

        result.InRange = currentDistance <= requiredRange;
        if (!result.InRange)
        {
            round.Log.Add($"{attacker.DisplayName} is out of range (dist={currentDistance}, range={requiredRange}).");
            return result;
        }

        // --- Determine target location ---
        // The defender's generic card body part determines where they get hit
        result.TargetLocation = defenderPair.Generic != null
            ? FighterInstance.BodyPartToLocation(defenderPair.Generic.BodyPart)
            : BodyLocation.Torso; // Default if no generic (shouldn't normally occur)

        // --- Power vs Defense ---
        int attackerPower = attacker.GetCardPower(attackerPair.Generic ?? (CardBase)attackerPair.Special!)
                          + attacker.GetCardPower(attackerPair.Unique ?? (CardBase)attackerPair.Special!);

        int defenderDefense = defender.GetCardDefense(defenderPair.Generic ?? (CardBase)defenderPair.Special!)
                            + defender.GetCardDefense(defenderPair.Unique ?? (CardBase)defenderPair.Special!);

        // --- Keyword modifiers ---
        var atkKeywords = attackerPair.AllKeywords.ToList();

        // ArmorBreak: reduce defender defense by 1
        if (atkKeywords.ContainsKeyword(CardKeyword.ArmorBreak))
        {
            defenderDefense = Math.Max(0, defenderDefense - 1);
            result.TriggeredKeywords.Add(CardKeyword.ArmorBreak);
        }

        // Piercing: ignore half defender defense
        if (atkKeywords.ContainsKeyword(CardKeyword.Piercing))
        {
            defenderDefense = defenderDefense / 2;
            result.TriggeredKeywords.Add(CardKeyword.Piercing);
        }

        // Guard keyword on defender: +2 defense
        var defKeywords = defenderPair.AllKeywords.ToList();
        if (defKeywords.ContainsKeyword(CardKeyword.Guard))
        {
            defenderDefense += 2;
            result.TriggeredKeywords.Add(CardKeyword.Guard);
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
        // 1 step per 2 net power (minimum 1 if landed)
        result.NetDamageSteps = Math.Max(1, net / 2);

        // Crushing keyword: +1 damage step
        if (atkKeywords.ContainsKeyword(CardKeyword.Crushing))
        {
            result.NetDamageSteps++;
            result.TriggeredKeywords.Add(CardKeyword.Crushing);
        }

        round.Log.Add(
            $"{attacker.DisplayName} hits {defender.DisplayName}'s {result.TargetLocation} " +
            $"for {result.NetDamageSteps} damage step(s). (power={attackerPower}, def={defenderDefense})");

        // --- Apply keywords that modify target ---
        if (atkKeywords.ContainsKeyword(CardKeyword.Bleed))
        {
            defender.LocationStates[result.TargetLocation].BleedStacks++;
            result.TriggeredKeywords.Add(CardKeyword.Bleed);
            round.Log.Add($"  {defender.DisplayName}'s {result.TargetLocation} is now bleeding!");
        }

        if (atkKeywords.ContainsKeyword(CardKeyword.Stagger))
        {
            defender.StaggerTurnsRemaining = 1;
            result.TriggeredKeywords.Add(CardKeyword.Stagger);
            round.Log.Add($"  {defender.DisplayName} is staggered — cooldowns +1 next turn.");
        }

        if (atkKeywords.ContainsKeyword(CardKeyword.Knockback))
        {
            result.TriggeredKeywords.Add(CardKeyword.Knockback);
            round.Log.Add($"  {defender.DisplayName} is knocked back 1 hex.");
        }

        // --- Apply damage ---
        defender.LocationStates[result.TargetLocation].ApplyDamage(result.NetDamageSteps);

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
