namespace Grants.Models.Fighter;

/// <summary>
/// Damage locations on a fighter's body. Each maps to a GenericCard.
/// </summary>
public enum BodyLocation
{
    Head,
    Torso,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg,
    Core,
    Stance,
}

/// <summary>
/// Current damage state of a body location.
/// Progression is always: Healthy → Bruised → Injured → Disabled
/// Some fighters/upgrades can cap a location (e.g., can never exceed Injured).
/// </summary>
public enum DamageState
{
    Healthy  = 0,
    Bruised  = 1,
    Injured  = 2,
    Disabled = 3,
}

/// <summary>
/// Stat penalty applied at each damage state for the corresponding generic card.
/// </summary>
public static class DamageStatPenalty
{
    public static int PowerPenalty(DamageState state) => state switch
    {
        DamageState.Healthy  => 0,
        DamageState.Bruised  => 1,
        DamageState.Injured  => 2,
        DamageState.Disabled => 99, // card removed from hand
        _ => 0,
    };

    public static int DefensePenalty(DamageState state) => state switch
    {
        DamageState.Healthy  => 0,
        DamageState.Bruised  => 1,
        DamageState.Injured  => 1,
        DamageState.Disabled => 99,
        _ => 0,
    };

    public static int SpeedPenalty(DamageState state) => state switch
    {
        DamageState.Healthy  => 0,
        DamageState.Bruised  => 0,
        DamageState.Injured  => 1,
        DamageState.Disabled => 99,
        _ => 0,
    };
}

/// <summary>
/// Tracks the live damage state of one body location during a match.
/// </summary>
public class LocationState
{
    public BodyLocation Location { get; init; }
    public DamageState State { get; set; } = DamageState.Healthy;

    /// <summary>
    /// If set, this location cannot progress past this cap (e.g., upgrade "Knee Brace" caps Leg at Injured).
    /// </summary>
    public DamageState? DamageCap { get; set; } = null;

    /// <summary>Bleed stacks — adds +1 incoming damage per stack at start of opponent's next turn.</summary>
    public int BleedStacks { get; set; } = 0;

    public bool IsAvailable => State != DamageState.Disabled;

    public void ApplyDamage(int steps = 1)
    {
        int next = (int)State + steps;
        if (DamageCap.HasValue)
            next = Math.Min(next, (int)DamageCap.Value);
        State = (DamageState)Math.Min(next, (int)DamageState.Disabled);
    }

    /// <summary>Reverses up to <paramref name="steps"/> damage steps (cannot go below Healthy).</summary>
    public void ReduceDamage(int steps = 1)
    {
        State = (DamageState)Math.Max(0, (int)State - steps);
    }
}
