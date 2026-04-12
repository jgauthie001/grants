using Grants.Engine;
using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;

namespace Grants.Fighters.Cursed;

/// <summary>
/// The Catalyst persona — an alternate take on The Cursed's token system.
///
/// Core differences from CursedPersona:
///
///   Opponent tokens (same transfer mechanic as CursedPersona):
///     - Every time The Catalyst lands a hit they build their pool and transfer
///       a token to the opponent (same pool/overflow rules).
///     - HOWEVER, when the opponent spends a token they get +1 Power / +1 Speed
///       (a BUFF, not a penalty). Token is consumed immediately — no refund mechanic.
///     - This makes the opponent WANT to collect tokens, creating rivalry over
///       whether transferring tokens is good for The Catalyst.
///
///   Self-spend (new mechanic, unique to this persona):
///     - Before card selection, The Catalyst may spend 1 token from their own pool
///       for +2 Power / +2 Speed that round.
///     - This consumes the token permanently.
///     - The Catalyst must choose: hoard pool tokens for the self-spend burst, or
///       keep transferring and let the opponent benefit.
///
/// Card keywords work identically to CursedPersona:
///   CurseGain, CursePull, CurseEmpower, CurseWeaken
/// </summary>
public class CatalystPersona : FighterPersona
{
    public static readonly CatalystPersona Instance = new()
    {
        PersonaId = "catalyst",
        Name     = "The Catalyst",
        Description = "Curse tokens empower whoever spends them — but The Catalyst spends harder.",
    };

    private CatalystPersona() { }

    private const string KeyPool   = "cursed_pool";
    private const string KeyTokens = "curse_tokens"; // stored on OPPONENT's PersonaState
    private const int    MaxPool   = 3;
    private const int    MaxTokens = 3;

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
    }

    // ─── Round complete ───────────────────────────────────────────────────────

    public override void OnRoundResolutionComplete(
        RoundState round,
        MatchState match,
        FighterInstance ownerFighter,
        FighterInstance opponent,
        PersonaState state) { }

    // ─── Opponent choice hooks (spend = buff) ────────────────────────────────

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
        return $"You carry {tokens} Curse token(s). Spend 1 for +1 Power/+1 Speed this round?  [Pool: {pool}/{MaxPool}]";
    }

    public override bool ResolveAiOpponentChoice(
        FighterInstance owner,
        FighterInstance opponent,
        MatchState match,
        PersonaState state)
        // AI always spends when it's a buff — unless saving tokens isn't a thing here (no refund)
        => opponent.PersonaState.Counters.GetValueOrDefault(KeyTokens, 0) > 0;

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

        // Spend 1 token: opponent gains +1 Power / +1 Speed this round
        opponent.PersonaState.Counters[KeyTokens] = tokens - 1;
        opponent.RoundPowerModifier += 1;
        opponent.RoundSpeedModifier += 1;
    }

    // ─── Self-choice hooks (spend from pool = big buff) ───────────────────────

    public override bool RequiresSelfRoundStartChoice(
        FighterInstance owner,
        FighterInstance opponent,
        MatchState match,
        PersonaState state)
        => state.Counters.GetValueOrDefault(KeyPool, 0) > 0;

    public override string GetSelfChoicePrompt(
        FighterInstance owner,
        FighterInstance opponent,
        PersonaState state)
    {
        int pool = state.Counters.GetValueOrDefault(KeyPool, 0);
        return $"Your pool holds {pool} token(s). Spend 1 for +2 Power/+2 Speed this round?  [Pool: {pool}/{MaxPool}]";
    }

    public override bool ResolveAiSelfChoice(
        FighterInstance owner,
        FighterInstance opponent,
        MatchState match,
        PersonaState state)
    {
        // AI spends when pool is at 2+ (save 1 as seed) or when injured (needs urgency)
        int pool    = state.Counters.GetValueOrDefault(KeyPool, 0);
        int injured = owner.LocationStates.Values.Count(ls => ls.State >= DamageState.Injured);
        return pool >= 2 || (pool >= 1 && injured >= 2);
    }

    public override void OnSelfChoice(
        FighterInstance owner,
        FighterInstance opponent,
        bool accepted,
        MatchState match,
        PersonaState state)
    {
        if (!accepted) return;

        int pool = state.Counters.GetValueOrDefault(KeyPool, 0);
        if (pool <= 0) return;

        state.Counters[KeyPool]    = pool - 1;
        owner.RoundPowerModifier  += 2;
        owner.RoundSpeedModifier  += 2;
    }

    // ─── HUD ──────────────────────────────────────────────────────────────────

    public override List<string> GetHudDisplayInfo(PersonaState state)
    {
        int pool = state.Counters.GetValueOrDefault(KeyPool, 0);
        return new() { $"CATALYST | Pool: {pool}/{MaxPool}" };
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
            // Overflow: 2 damage steps to 2 random non-disabled, non-Stance locations
            round.Log.Add($"[The Catalyst] {owner.DisplayName}'s pool is full! Overflow deals 2 damage!");
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

        state.Counters[KeyPool]                   = pool   - 1;
        opponent.PersonaState.Counters[KeyTokens] = tokens + 1;
        round.Log.Add($"[The Catalyst] {opponent.DisplayName} receives a Curse token! ({tokens + 1}/{MaxTokens})");
    }

    private static Random GetRng(PersonaState state)
    {
        if (state.CustomData.TryGetValue("rng", out var r) && r is Random rng) return rng;
        var newRng = new Random();
        state.CustomData["rng"] = newRng;
        return newRng;
    }
}
