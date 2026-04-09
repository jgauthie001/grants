using Grants.Models.Board;
using Grants.Models.Match;
using Grants.Models.Fighter;

namespace Grants.Models.Stage;

/// <summary>
/// Abstract base class for stage modifiers — environmental effects that change gameplay.
/// Stages define the arena rules, hazards, and dynamic effects that persist across all rounds.
/// 
/// Examples:
/// - ShrinkingStage: Arena shrinks each round; invalid cells push fighters toward center
/// - HazardousStage: Certain hexes deal damage when occupied or traversed
/// - OutOfBoundsStage: Standing outside marked boundary incurs penalties
/// - WeatherStage: Environmental effects (wind affects movement, lightning strikes, etc.)
/// </summary>
public abstract class StageModifier
{
    public string StageId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Create the board layout for this stage.
    /// Return a custom board, or null to use the default 7x7 hex board.
    /// </summary>
    public virtual HexBoard CreateBoard()
    {
        return new HexBoard();
    }

    /// <summary>
    /// Initialize runtime state for this stage at match start.
    /// Called once when MatchState is created.
    /// </summary>
    public abstract StageState CreateRuntimeState();

    /// <summary>
    /// Called at the start of each round (before card selection).
    /// Allows stage to modify the board state or apply persistent effects.
    /// </summary>
    public abstract void OnRoundStart(
        MatchState match,
        StageState state);

    /// <summary>
    /// Called after a fighter completes movement.
    /// Can apply damage, knockback, or status effects based on position.
    /// Return true if the fighter should be moved/knocked back.
    /// </summary>
    public virtual bool OnFighterMovementComplete(
        HexCoord newPosition,
        FighterInstance fighter,
        MatchState match,
        StageState state)
    {
        return false; // Default: no effect
    }

    /// <summary>
    /// Called before attacks are resolved each round.
    /// Can modify damage, apply range penalties, etc. based on stage effects.
    /// </summary>
    public virtual void OnAttackPhaseStart(
        RoundState round,
        MatchState match,
        StageState state)
    {
        // Default: no modification
    }

    /// <summary>
    /// Called at the end of each round (after attacks resolve).
    /// Apply end-of-round effects, decay hazards, update stage state.
    /// </summary>
    public virtual void OnRoundComplete(
        RoundState round,
        MatchState match,
        StageState state)
    {
        // Default: no post-round logic
    }

    /// <summary>
    /// Return dangerous/restricted hexes that should display warnings on HUD.
    /// </summary>
    public virtual List<HexCoord> GetHazardousHexes(StageState state)
    {
        return new();
    }

    /// <summary>
    /// Return UI display info for this stage's current effects.
    /// Displayed in match HUD to show active stage mechanics.
    /// </summary>
    public virtual List<string> GetHudDisplayInfo(StageState state)
    {
        return new();
    }
}

/// <summary>
/// Runtime state container for a stage. Tracks dynamic effects, timers, and hazard positions.
/// </summary>
public class StageState
{
    /// <summary>Turn counter for stage (some effects scale with time).</summary>
    public int TurnCount { get; set; } = 0;

    /// <summary>Stage-specific cooldowns/timers (effect name -> turns remaining).</summary>
    public Dictionary<string, int> EffectTimers { get; set; } = new();

    /// <summary>Hazard positions (hex coord -> hazard type).</summary>
    public Dictionary<HexCoord, string> HazardMap { get; set; } = new();

    /// <summary>Restricted/invalid hexes (e.g., shrunken arena boundary).</summary>
    public HashSet<HexCoord> RestrictedCells { get; set; } = new();

    /// <summary>Stage-specific flags (one-time triggers, active modes).</summary>
    public Dictionary<string, bool> Flags { get; set; } = new();

    /// <summary>Generic metadata for stage extensions.</summary>
    public Dictionary<string, object> CustomData { get; set; } = new();

    /// <summary>Decrement all timers.</summary>
    public void DecrementTimers()
    {
        var keys = EffectTimers.Keys.ToList();
        foreach (var key in keys)
        {
            if (EffectTimers[key] > 0)
                EffectTimers[key]--;
        }
    }

    /// <summary>Set a timer for an effect.</summary>
    public void SetEffectTimer(string effectName, int turns)
    {
        EffectTimers[effectName] = turns;
    }
}

/// <summary>
/// Hazard types that can be placed on the board.
/// Used to categorize what happens when a fighter enters that hex.
/// </summary>
public enum HazardType
{
    None,
    Damage,           // Takes damage on entry
    Stun,             // Slowed or stunned
    Knockback,        // Knocked back toward center
    OutOfBounds,      // Push or penalty zone
    Ice,              // Reduced movement
    Fire,             // Damage per turn
    Trap,             // Similar to persona traps but arena-wide
}

/// <summary>
/// Represents a damage or effect zone on the board.
/// </summary>
public class HazardZone
{
    public HexCoord Position { get; set; }
    public HazardType Type { get; set; }
    public int Intensity { get; set; } = 1; // Damage amount, stun duration, etc.
    public int TurnsRemaining { get; set; } = -1; // -1 = permanent
}
