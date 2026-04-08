using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Match;

namespace Grants.Models.Fighter;

/// <summary>
/// Example persona: A fighter who specializes in interrupting opponent actions.
/// 
/// Mechanics (design phase, customize as needed):
/// - Can interrupt opponent's attack after cards are revealed but before resolution
/// - Gains stacks from successful defensive actions (blocks, dodges)
/// - Spends stacks to force opponent to re-select card pairs
/// - High risk/reward: successful interrupts disable opponent, but failed interrupts grant bonus damage
/// 
/// Example card tree:
/// - "Counter" (passive) — interrupt window opens when opponent attacks
/// - "Bait" — tempt opponent to attack, then dodge and trigger interrupt
/// - Items: "Quick Reflexes" (interrupt costs 1 fewer stack), "Evasion Master" (interrupt CD -1)
/// </summary>
public class DisruptorPersona : FighterPersona
{
    public static readonly DisruptorPersona Instance = new()
    {
        PersonaId = "disruptor",
        Name = "Disruptor",
        Description = "Interrupts enemy attacks and turns their aggression against them."
    };

    private DisruptorPersona() { }

    public override PersonaState CreateRuntimeState()
    {
        var state = new PersonaState();
        // Track interrupt availability
        state.SetAbilityCooldown("interrupt", 0);
        state.SetAbilityCooldown("bait", 0);
        // Track interrupt stacks (currency for interrupt ability)
        state.Counters["interrupt_stacks"] = 0;
        return state;
    }

    public override CardPair ModifyCardSelection(
        CardPair selectedPair,
        FighterInstance fighter,
        PersonaState state)
    {
        // [TODO] If interrupt_active flag is set, allow special "interrupt" card selection
        // that overrides normal card pair
        return selectedPair;
    }

    public override int ModifyCardStat(
        CardBase card,
        StatType stat,
        int baseValue,
        FighterInstance fighter,
        PersonaState state)
    {
        // [TODO] Defensive cards gain bonuses when interrupt stacks are high
        if (stat == StatType.Defense && state.Counters.TryGetValue("interrupt_stacks", out int stacks))
        {
            if (stacks > 0)
                return baseValue + stacks; // Each stack adds +1 defense
        }
        return baseValue;
    }

    public override CardPair? GetPersonalizedAiDecision(
        FighterInstance ai,
        FighterInstance opponent,
        HexBoard board,
        PersonaState state)
    {
        // [TODO] AI strategy: if opponent has been aggressive (damaged us > N), bait next turn
        // Otherwise, build stacks defensively
        return null; // Use default AI for now
    }

    public override void OnRoundResolutionStart(
        RoundState round,
        MatchState match,
        FighterInstance ownerFighter,
        FighterInstance opponent,
        PersonaState state)
    {
        // [TODO] After card pairs are selected but before resolution:
        // - Emit an interrupt decision window
        // - If interrupt is attempted: cost stacks, modify round outcome
        // - If interrupt succeeds: opponent re-selects card pair
        // - If interrupt fails: opponent gains +X damage this round

        // Example stub:
        if (state.IsAbilityReady("interrupt") && 
            state.Counters.TryGetValue("interrupt_stacks", out int stacks) &&
            stacks >= 1)
        {
            // [TODO] Prompt player: "Interrupt for X stacks?" Y/N
            // Log: "Attempting to interrupt..."
        }
    }

    public override void OnRoundResolutionComplete(
        RoundState round,
        MatchState match,
        FighterInstance ownerFighter,
        FighterInstance opponent,
        PersonaState state)
    {
        // [TODO] Award stacks on successful defensive plays
        // Examples:
        // - Opponent hit us but we landed a block -> +1 stack
        // - Opponent missed -> +1 stack
        // - We took damage -> +0.5 stack

        // Stub: always gain 1 stack (customize logic)
        if (state.Counters.TryGetValue("interrupt_stacks", out int stacks))
        {
            state.Counters["interrupt_stacks"] = Math.Min(stacks + 1, 5); // Cap at 5
        }
    }

    public override void UpdateState(PersonaState state)
    {
        // Decrement ability cooldowns
        state.DecrementCooldowns();

        // [TODO] Passive stack decay: lose 1 stack per 2 rounds of inactivity
        // (reset counter if attack was landed this round)
        
        // Decay interrupt stacks if full action AP (active protection) expires
        if (state.Flags.TryGetValue("ap_active", out bool apActive) && apActive)
        {
            // AP duration check — remove AP after N turns
            state.Flags["ap_active"] = false;
        }
    }

    public override List<string> GetHudDisplayInfo(PersonaState state)
    {
        var info = new List<string>();

        // Show interrupt stack count
        if (state.Counters.TryGetValue("interrupt_stacks", out int stacks))
        {
            info.Add($"Interrupt Stacks: {stacks}/5");
        }

        // Show ability readiness
        if (!state.IsAbilityReady("interrupt"))
            info.Add($"Interrupt Ready: {state.AbilityCooldowns["interrupt"]} turns");

        return info;
    }
}
