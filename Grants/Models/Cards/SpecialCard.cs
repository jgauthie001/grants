namespace Grants.Models.Cards;

/// <summary>
/// Special card — high-impact, fighter-defining. Two per fighter.
/// Base cooldown: 3 turns. Typically slow speed (-1 or -2), high power.
/// May have positioning requirements or prerequisite opponent states.
/// 
/// Range System: Defines MIN and MAX hex distance for this special attack.
/// Example: MinRange=1, MaxRange=3 means "can hit 1, 2, or 3 hexes away"
/// Special cards don't pair with generics (unless Standalone=false), so their range is fixed.
/// </summary>
public class SpecialCard : CardBase
{
    /// <summary>
    /// Minimum range in hexes. Attack can hit opponents at least this far away.
    /// </summary>
    public int MinRange { get; init; } = 1;

    /// <summary>
    /// Maximum range in hexes. Attack cannot hit opponents farther than this.
    /// </summary>
    public int MaxRange { get; init; } = 1;

    /// <summary>
    /// Optional prerequisite: a specific body location on the opponent must be
    /// at this damage state or worse. e.g., "Head must be Bruised".
    /// </summary>
    public Fighter.BodyLocation? RequiresOpponentLocationDamaged { get; init; }
    public Fighter.DamageState RequiresOpponentMinState { get; init; } = Fighter.DamageState.Healthy;

    /// <summary>
    /// If true, this special does not need a generic card paired with it.
    /// It fires as a standalone action.
    /// </summary>
    public bool Standalone { get; init; } = false;
}
