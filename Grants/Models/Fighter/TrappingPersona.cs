using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Match;

namespace Grants.Models.Fighter;

/// <summary>
/// Example persona: A fighter who specializes in trapping and controlling the board.
/// 
/// Mechanics (design phase, customize as needed):
/// - Can place traps on hex tiles (consumes action or passive ability)
/// - Traps remain until triggered or expire
/// - Triggers on opponent movement: can knock back, stun, or disable location
/// - Can activate traps manually for area denial / zoning
/// 
/// Example ability tree:
/// - "Place Trap" (2 turn cooldown) — place at a distance >= 1
/// - "Push" (1 turn cooldown) — knock adjacent opponent 1-2 hexes away
/// - Items: "Tripwire Master" (traps trigger twice), "Stubborn Ground" (resist push)
/// </summary>
public class TrappingPersona : FighterPersona
{
    public static readonly TrappingPersona Instance = new()
    {
        PersonaId = "trapper",
        Name = "Trapmaster",
        Description = "Places traps on the board and controls positioning through area denial."
    };

    private TrappingPersona() { }

    public override PersonaState CreateRuntimeState()
    {
        var state = new PersonaState();
        // Initialize ability cooldowns
        state.SetAbilityCooldown("place_trap", 0);
        state.SetAbilityCooldown("push", 0);
        // Custom data: track trap positions and their types
        state.CustomData["trap_positions"] = new List<HexCoord>();
        return state;
    }

    public override CardPair ModifyCardSelection(
        CardPair selectedPair,
        FighterInstance fighter,
        PersonaState state)
    {
        // [TODO] Could check if a "trap_active" flag is set and redirect to trap card
        return selectedPair;
    }

    public override int ModifyCardStat(
        CardBase card,
        StatType stat,
        int baseValue,
        FighterInstance fighter,
        PersonaState state)
    {
        // [TODO] Example: boost trap-related cards (special semantic check on card name/keywords)
        if (stat == StatType.Movement && card.Id.Contains("trap"))
            return baseValue + 2; // Traps gain extra range/movement
        return baseValue;
    }

    public override CardPair? GetPersonalizedAiDecision(
        FighterInstance ai,
        FighterInstance opponent,
        HexBoard board,
        PersonaState state)
    {
        // [TODO] Custom AI: if traps damaged opponent recently, prioritize push ability
        // Otherwise, place traps preemptively when opponent is far away
        // Return null to use default AI
        return null;
    }

    public override void OnRoundResolutionStart(
        RoundState round,
        MatchState match,
        FighterInstance ownerFighter,
        FighterInstance opponent,
        PersonaState state)
    {
        // [TODO] Check if opponent moved into trap zone
        // If yes: trigger trap effect (knockback / stun / disable location)
        // Log trap activation
    }

    public override void UpdateState(PersonaState state)
    {
        // Decrement ability cooldowns each turn
        state.DecrementCooldowns();

        // Expire traps that have been on board > N turns
        if (state.CustomData.TryGetValue("trap_positions", out var trapsObj) &&
            trapsObj is List<HexCoord> traps)
        {
            // [TODO] Implement trap expiration logic
        }

        // Decay any "threat markers" or charge counters
        state.UpdateCounter("threat_stacks", -1);
    }

    public override List<string> GetHudDisplayInfo(PersonaState state)
    {
        var info = new List<string>();
        
        // Show trap count
        if (state.CustomData.TryGetValue("trap_count", out var countObj) &&
            countObj is int trapCount)
        {
            info.Add($"Traps: {trapCount}");
        }

        // Show ability cooldowns
        if (!state.IsAbilityReady("place_trap"))
            info.Add($"Place Trap: {state.AbilityCooldowns["place_trap"]} turns");
        if (!state.IsAbilityReady("push"))
            info.Add($"Push: {state.AbilityCooldowns["push"]} turns");

        return info;
    }
}
