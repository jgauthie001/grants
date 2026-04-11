using Grants.Engine;
using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;

namespace Grants.Fighters.Cursed;

/// <summary>
/// The Cursed persona. Character-specific mechanics:
///
/// Pool: The Cursed has a private pool of Curse tokens (0-3).
///   - Every time The Cursed lands a hit, they gain 1 token to their pool.
///   - If the pool is already full (3), overflow deals 2 damage steps to 2 random
///     (possibly same) non-disabled, non-Stance locations instead of gaining.
///   - After gaining, 1 token is transferred from the pool to the opponent
///     (if the pool has tokens and the opponent is below their 3-token cap).
///   - Cards with the CurseGain keyword gain 1 EXTRA token to the pool on hit.
///
/// Opponent tokens:
///   - The opponent holds up to 3 Curse tokens on their PersonaState.
///   - At the start of each round (before card selection), the opponent is offered:
///     "Spend a Curse token for -1 Power and -1 Speed this round?"
///   - If the opponent declines that offer (or has no tokens) and The Cursed hits them
///     that round, 1 token is transferred as normal.
///   - If the opponent ACCEPTS the spend:
///     - They take -1 Power / -1 Speed (via RoundPowerModifier / RoundSpeedModifier).
///     - If they also land a hit on The Cursed that round, the token is consumed.
///     - If they do NOT land a hit on The Cursed, the token is returned to The Cursed's pool.
///
/// Card keywords (handled in AttackEngine / ResolutionEngine):
///   CurseGain    — +1 extra pool token on hit (on top of base gain)
///   CursePull    — pull opponent N hexes toward self (N = their curse token count)
///   CurseEmpower — +N power this attack (N = The Cursed's pool count)
///   CurseWeaken  — -N defense this attack (N = opponent's curse tokens)
/// </summary>
public class CursedPersona : FighterPersona
{
    public static readonly CursedPersona Instance = new()
    {
        PersonaId = "cursed",
        Name = "The Cursed",
        Description = "Builds a pool of Curse tokens on every hit, then transfers them to the opponent.",
    };

    private CursedPersona() { }

    private const string KeyPool       = "cursed_pool";
    private const string KeyTokens     = "curse_tokens";   // stored on OPPONENT's PersonaState
    private const string KeySpent      = "curse_spent";    // set on opponent when they spend a token
    private const string KeyHitCursed  = "curse_hit_cursed"; // set on opponent when they land on The Cursed
    private const int    MaxPool       = 3;
    private const int    MaxTokens     = 3;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public override PersonaState CreateRuntimeState()
    {
        var state = new PersonaState();
        state.Counters[KeyPool] = 0;
        state.CustomData["rng"] = new Random();
        return state;
    }

    public override CardPair ModifyCardSelection(CardPair selectedPair, FighterInstance fighter, PersonaState state)
        => selectedPair;

    public override CardPair? GetPersonalizedAiDecision(FighterInstance ai, FighterInstance opponent, HexBoard board, PersonaState state)
        => null;

    public override void OnRoundResolutionStart(RoundState round, MatchState match, FighterInstance ownerFighter, FighterInstance opponent, PersonaState state)
    {
        // Nothing special at resolution start
    }

    // ─── Hit hook ─────────────────────────────────────────────────────────────

    public override void OnLandedHit(
        FighterInstance attacker,
        FighterInstance defender,
        CardPair attackerPair,
        AttackEngine.AttackResult result,
        RoundState round,
        MatchState match,
        PersonaState state)
    {
        bool ownerIsAttacker = object.ReferenceEquals(state, attacker.PersonaState);
        bool ownerIsDefender = object.ReferenceEquals(state, defender.PersonaState);

        if (ownerIsAttacker)
        {
            // Base gain: +1 to pool
            TryGainPoolToken(attacker, round, state);

            // CurseGain keyword: +1 extra
            if (result.TriggeredKeywords.ContainsKeyword(CardKeyword.CurseGain))
                TryGainPoolToken(attacker, round, state);

            // Transfer: 1 from pool to opponent
            TryTransferToken(attacker, defender, round, state);
        }

        if (ownerIsDefender)
        {
            // Mark on attacker's state: they hit The Cursed this round (used for spend refund)
            attacker.PersonaState.Flags[KeyHitCursed] = true;
        }
    }

    // ─── Round complete ───────────────────────────────────────────────────────

    public override void OnRoundResolutionComplete(
        RoundState round,
        MatchState match,
        FighterInstance ownerFighter,
        FighterInstance opponent,
        PersonaState state)
    {
        // Resolve pending spend: did the opponent spend a token this round?
        bool spent = opponent.PersonaState.Flags.TryGetValue(KeySpent, out bool s) && s;
        if (spent)
        {
            bool hitCursed = opponent.PersonaState.Flags.TryGetValue(KeyHitCursed, out bool h) && h;
            if (hitCursed)
            {
                round.Log.Add($"[The Cursed] {opponent.DisplayName} hit The Cursed -- Curse token consumed.");
            }
            else
            {
                // Refund: return token to The Cursed's pool
                TryGainPoolToken(ownerFighter, round, state);
                round.Log.Add($"[The Cursed] {opponent.DisplayName} missed -- Curse token returned to {ownerFighter.DisplayName}'s pool.");
            }
            opponent.PersonaState.Flags[KeySpent]     = false;
            opponent.PersonaState.Flags[KeyHitCursed] = false;
        }
    }

    // ─── Opponent choice hooks ────────────────────────────────────────────────

    public override bool RequiresOpponentRoundStartChoice(
        FighterInstance owner,
        FighterInstance opponent,
        MatchState match,
        PersonaState state)
        => opponent.PersonaState.Counters.GetValueOrDefault(KeyTokens, 0) > 0;

    public override string GetOpponentChoicePrompt(
        FighterInstance owner,
        FighterInstance opponent,
        PersonaState state)
    {
        int tokens = opponent.PersonaState.Counters.GetValueOrDefault(KeyTokens, 0);
        int pool   = state.Counters.GetValueOrDefault(KeyPool,   0);
        return $"You carry {tokens} Curse token(s). Spend 1 for -1 Power/-1 Speed? " +
               $"(token returned if you don't hit The Cursed)  [Pool: {pool}/{MaxPool}]";
    }

    public override bool ResolveAiOpponentChoice(
        FighterInstance owner,
        FighterInstance opponent,
        MatchState match,
        PersonaState state)
    {
        // AI strategy: spend token if they have 2+ tokens (reduce curse burden) AND
        // they have reasonable attack power to absorb the -1 penalty.
        int tokens  = opponent.PersonaState.Counters.GetValueOrDefault(KeyTokens, 0);
        int injured = opponent.LocationStates.Values.Count(ls => ls.State >= DamageState.Injured);
        return tokens >= 2 && injured < 3;
    }

    public override void OnOpponentChoice(
        FighterInstance owner,
        FighterInstance opponent,
        bool accepted,
        MatchState match,
        PersonaState state)
    {
        if (!accepted) return;

        int tokens = opponent.PersonaState.Counters.GetValueOrDefault(KeyTokens, 0);
        if (tokens <= 0) return;

        opponent.PersonaState.Counters[KeyTokens] = tokens - 1;
        opponent.PersonaState.Flags[KeySpent]     = true;
        opponent.RoundPowerModifier -= 1;
        opponent.RoundSpeedModifier -= 1;
    }

    // ─── HUD ──────────────────────────────────────────────────────────────────

    public override List<string> GetHudDisplayInfo(PersonaState state)
    {
        int pool = state.Counters.GetValueOrDefault(KeyPool, 0);
        if (pool == 0 && !state.Counters.ContainsKey(KeyPool))
            return new();
        return new() { $"THE CURSED | Pool: {pool}/{MaxPool}" };
    }

    // ─── Internals ────────────────────────────────────────────────────────────

    private static void TryGainPoolToken(FighterInstance owner, RoundState round, PersonaState state)
    {
        int pool = state.Counters.GetValueOrDefault(KeyPool, 0);
        if (pool < MaxPool)
        {
            state.Counters[KeyPool] = pool + 1;
        }
        else
        {
            // Overflow: 2 damage steps to 2 random (possibly same) non-disabled, non-Stance locations
            round.Log.Add($"[The Cursed] {owner.DisplayName}'s pool is full! Overflow deals 2 damage!");
            var rng = GetRng(state);
            var targetable = owner.LocationStates.Values
                .Where(ls => ls.State != DamageState.Disabled && ls.Location != BodyLocation.Stance)
                .ToList();
            if (targetable.Count == 0) return;
            for (int i = 0; i < 2; i++)
            {
                var loc = targetable[rng.Next(targetable.Count)];
                loc.ApplyDamage(1);
                round.Log.Add($"  Overflow: {owner.DisplayName}'s {loc.Location} takes 1 damage. ({loc.State})");
            }
        }
    }

    private static void TryTransferToken(
        FighterInstance owner,
        FighterInstance opponent,
        RoundState round,
        PersonaState state)
    {
        int pool   = state.Counters.GetValueOrDefault(KeyPool,   0);
        int tokens = opponent.PersonaState.Counters.GetValueOrDefault(KeyTokens, 0);

        if (pool <= 0 || tokens >= MaxTokens) return;
        if (opponent.ActiveImmunities.Contains(CombatImmunity.CurseToken)) return;

        state.Counters[KeyPool]                    = pool   - 1;
        opponent.PersonaState.Counters[KeyTokens]  = tokens + 1;
        round.Log.Add($"[The Cursed] {opponent.DisplayName} receives a Curse token! ({tokens + 1}/{MaxTokens})");
    }

    private static Random GetRng(PersonaState state)
    {
        if (state.CustomData.TryGetValue("rng", out var r) && r is Random rng) return rng;
        var newRng = new Random();
        state.CustomData["rng"] = newRng;
        return newRng;
    }
}
