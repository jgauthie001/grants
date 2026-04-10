namespace Grants.Models.Cards;

/// <summary>
/// Generic card — represents a body part being used. One per functional body part in hand.
/// Available only if the corresponding body location is NOT Disabled.
/// Base cooldown: 1 turn.
/// 
/// Range Interaction: GenericCard provides MIN and MAX range modifiers.
/// These are added to the paired unique/special card's range to determine effective attack range.
/// 
/// Example:
/// - Unique card: MinRange=1, MaxRange=2
/// - Generic card: MinRangeModifier=0, MaxRangeModifier=+1
/// - Effective range: 1 to 3 hexes
/// 
/// Interpretation:
/// - MinRangeModifier: How much the minimum range can be extended (usually 0 or negative)
/// - MaxRangeModifier: How much the maximum range can be extended (usually 0 or positive)
/// - Negative modifiers reduce range (defensive stance)
/// - Positive modifiers increase range (aggressive reach)
/// </summary>
public class GenericCard : CardBase
{
    /// <summary>The body part this card represents.</summary>
    public BodyPart BodyPart { get; init; }

    /// <summary>
    /// Which unique card body part requirements this card satisfies.
    /// e.g., LeftArm satisfies "requires arm", Legs satisfy "requires leg"
    /// </summary>
    public List<string> SatisfiesTags { get; init; } = new();

    /// <summary>
    /// Modifier applied to the minimum range of the paired unique/special card.
    /// Default 0 means no change to minimum range.
    /// -1 means minimum range is reduced by 1 (e.g., 2→1, requires getting closer)
    /// +1 means minimum range is increased by 1 (e.g., works from farther away)
    /// </summary>
    public int MinRangeModifier { get; set; } = 0;

    /// <summary>
    /// Modifier applied to the maximum range of the paired unique/special card.
    /// Default 0 means no change to maximum range.
    /// -1 means maximum range is reduced by 1 (e.g., 3→2, shorter reach)
    /// +1 means maximum range is increased by 1 (e.g., 3→4, extended reach)
    /// </summary>
    public int MaxRangeModifier { get; set; } = 0;
}
