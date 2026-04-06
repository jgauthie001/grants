namespace Grants.Models.Cards;

/// <summary>
/// Special card — high-impact, fighter-defining. Two per fighter.
/// Base cooldown: 3 turns. Typically slow speed (-1 or -2), high power.
/// May have positioning requirements or prerequisite opponent states.
/// </summary>
public class SpecialCard : CardBase
{
    /// <summary>
    /// Required range bracket for this special to be executable.
    /// null = no positional requirement.
    /// </summary>
    public RangeBracket? RequiredRange { get; init; }

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
