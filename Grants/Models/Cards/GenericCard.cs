namespace Grants.Models.Cards;

/// <summary>
/// Generic card — represents a body part being used. One per functional body part in hand.
/// Available only if the corresponding body location is NOT Disabled.
/// Base cooldown: 1 turn.
/// 
/// Range interaction: GenericCard provides a RangeModifier (-1, 0, +1, etc.)
/// paired with a UniqueCard's BaseRange to determine effective attack range.
/// Example: Long Jab (generic, +1 range) + Combo Strike (unique, Mid range)
///          = Effective range of Mid+1 (4 hexes)
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
    /// Range modifier for this generic card.
    /// Applied to the paired unique/special card's BaseRange.
    /// Examples:
    /// - 0: Neutral range (takes unique card's range)
    /// - +1: Reach increase (e.g., extended arm strike, forward step)
    /// - -1: Short range penalty (e.g., defensive crouch, pulling hand back)
    /// 
    /// Interpretation:
    /// - GenericCard with RangeModifier=+1 increases effective range by 1 bracket
    /// - GenericCard with RangeModifier=-1 decreases effective range by 1 bracket
    /// </summary>
    public int RangeModifier { get; init; } = 0;
}
