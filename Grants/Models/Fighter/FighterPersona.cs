using Grants.Engine;
using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Match;

namespace Grants.Models.Fighter;

/// <summary>
/// Abstract base class for fighter personas — character-specific gameplay mechanics.
/// A persona defines how a fighter plays: their AI strategy, special abilities, 
/// card modifications, and unique round resolution behavior.
/// 
/// Examples:
/// - TrappingPersona: Places traps on board, can prevent movement/push opponents
/// - DisruptorPersona: Can interrupt enemy attacks mid-round
/// - StandardPersona: Default balanced behavior (no special mechanics)
/// </summary>
public abstract class FighterPersona
{
    public string PersonaId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Called when the persona is activated for a fighter instance. 
    /// Initialize any runtime state here.
    /// </summary>
    public abstract PersonaState CreateRuntimeState();

    /// <summary>
    /// Modify or replace the card pair selection before it's finalized.
    /// Return the original pair to allow selection, or a different pair for special behavior.
    /// </summary>
    public abstract CardPair ModifyCardSelection(
        CardPair selectedPair,
        FighterInstance fighter,
        PersonaState state);

    /// <summary>
    /// Adjust a card's stat value based on persona effects.
    /// Called during pair resolution to apply bonuses/penalties.
    /// </summary>
    public virtual int ModifyCardStat(
        CardBase card,
        StatType stat,
        int baseValue,
        FighterInstance fighter,
        PersonaState state)
    {
        return baseValue; // Default: no modification
    }

    /// <summary>
    /// Return special action pairs or decision overrides for the AI.
    /// Called by AiEngine to allow persona to suggest unique selections.
    /// Return null to use default AI logic.
    /// </summary>
    public abstract CardPair? GetPersonalizedAiDecision(
        FighterInstance ai,
        FighterInstance opponent,
        HexBoard board,
        PersonaState state);

    /// <summary>
    /// Hook into round resolution to apply persona-specific mechanics.
    /// Called before attacks are resolved each round.
    /// Persona can modify round state, apply arena effects, trigger abilities, etc.
    /// </summary>
    public abstract void OnRoundResolutionStart(
        RoundState round,
        MatchState match,
        FighterInstance ownerFighter,
        FighterInstance opponent,
        PersonaState state);

    /// <summary>
    /// Hook after attacks are resolved to apply follow-up effects.
    /// Called after both fighters' attacks complete.
    /// </summary>
    public virtual void OnRoundResolutionComplete(
        RoundState round,
        MatchState match,
        FighterInstance ownerFighter,
        FighterInstance opponent,
        PersonaState state)
    {
        // Default: no post-round logic
    }

    /// <summary>
    /// Called immediately after a hit lands (attacker hurt defender).
    /// Called once for the attacker's persona and once for the defender's persona.
    /// Use to gain/transfer tokens, mark flags, react to being hit.
    /// </summary>
    public virtual void OnLandedHit(
        FighterInstance attacker,
        FighterInstance defender,
        CardPair attackerPair,
        AttackEngine.AttackResult result,
        RoundState round,
        MatchState match,
        PersonaState state) { }

    /// <summary>
    /// Return true if this persona wants to offer the OPPONENT a choice at round start
    /// (before card selection). Called for both fighters' personas each round.
    /// </summary>
    public virtual bool RequiresOpponentRoundStartChoice(
        FighterInstance owner,
        FighterInstance opponent,
        MatchState match,
        PersonaState state) => false;

    /// <summary>
    /// One-line prompt shown to the human opponent when a choice is required.
    /// </summary>
    public virtual string GetOpponentChoicePrompt(
        FighterInstance owner,
        FighterInstance opponent,
        PersonaState state) => "";

    /// <summary>
    /// AI decision when the opponent must choose. Return true to accept, false to decline.
    /// </summary>
    public virtual bool ResolveAiOpponentChoice(
        FighterInstance owner,
        FighterInstance opponent,
        MatchState match,
        PersonaState state) => false;

    /// <summary>
    /// Called once the opponent (human or AI) has made their choice.
    /// Use to apply stat modifiers (e.g. RoundPowerModifier) or set flags.
    /// </summary>
    public virtual void OnOpponentChoice(
        FighterInstance owner,
        FighterInstance opponent,
        bool accepted,
        MatchState match,
        PersonaState state) { }

    /// <summary>
    /// Called each turn to decrement or update persona-specific cooldowns/effects.
    /// For example: trap despawn timers, ability cooldowns, stacks decay.
    /// </summary>
    public virtual void UpdateState(PersonaState state)
    {
        // Default: no state updates
    }

    /// <summary>
    /// Return additional UI display info for this persona.
    /// Displayed in fight HUD to show active abilities, traps, etc.
    /// </summary>
    public virtual List<string> GetHudDisplayInfo(PersonaState state)
    {
        return new(); // Default: no additional info
    }
}

/// <summary>
/// Runtime state container for a persona. Tracks cooldowns, counters, and arena effects.
/// Each FighterInstance has one PersonaState per match.
/// </summary>
public class PersonaState
{
    /// <summary>Persona-specific cooldown tracking (ability name -> turns remaining).</summary>
    public Dictionary<string, int> AbilityCooldowns { get; set; } = new();

    /// <summary>Persona-specific counter/stack tracking (capacity name -> current value).</summary>
    public Dictionary<string, int> Counters { get; set; } = new();

    /// <summary>Arena-scoped effects (e.g., trap positions, markers, projectiles).</summary>
    public List<PersonaArenaEffect> ActiveEffects { get; set; } = new();

    /// <summary>Persona-specific flags (one-time triggers, mode toggles, etc.).</summary>
    public Dictionary<string, bool> Flags { get; set; } = new();

    /// <summary>Generic metadata storage for persona extensions.</summary>
    public Dictionary<string, object> CustomData { get; set; } = new();

    /// <summary>Check if an ability is off cooldown.</summary>
    public bool IsAbilityReady(string abilityName)
    {
        return !AbilityCooldowns.ContainsKey(abilityName) || AbilityCooldowns[abilityName] <= 0;
    }

    /// <summary>Apply cooldown to an ability.</summary>
    public void SetAbilityCooldown(string abilityName, int turns)
    {
        AbilityCooldowns[abilityName] = turns;
    }

    /// <summary>Decrement all cooldowns.</summary>
    public void DecrementCooldowns()
    {
        var keys = AbilityCooldowns.Keys.ToList();
        foreach (var key in keys)
        {
            if (AbilityCooldowns[key] > 0)
                AbilityCooldowns[key]--;
        }
    }

    /// <summary>Adjust a counter value (for stacks, charges, etc.).</summary>
    public void UpdateCounter(string counterName, int delta)
    {
        if (!Counters.ContainsKey(counterName))
            Counters[counterName] = 0;
        Counters[counterName] = Math.Max(0, Counters[counterName] + delta);
    }
}

/// <summary>
/// Represents an arena-scoped effect placed by a persona.
/// Examples: trap locations, projectile paths, intimidation zones.
/// </summary>
public class PersonaArenaEffect
{
    /// <summary>Unique ID for this effect instance.</summary>
    public string EffectId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Effect type label (e.g., "trap", "aura", "projectile").</summary>
    public string EffectType { get; set; } = string.Empty;

    /// <summary>Hex board position (if location-based).</summary>
    public HexCoord? Position { get; set; } = null;

    /// <summary>Turns until this effect expires (-1 = permanent).</summary>
    public int TurnsRemaining { get; set; } = 0;

    /// <summary>Persona-specific data payload.</summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>Stat types that personas can modify.</summary>
public enum StatType
{
    Power,
    Defense,
    Speed,
    Movement,
    Range,
}
