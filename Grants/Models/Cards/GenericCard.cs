namespace Grants.Models.Cards;

/// <summary>
/// Generic card — represents a body part being used. One per functional body part in hand.
/// Available only if the corresponding body location is NOT Disabled.
/// Base cooldown: 1 turn.
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
}
