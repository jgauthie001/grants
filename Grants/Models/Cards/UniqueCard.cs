namespace Grants.Models.Cards;

/// <summary>
/// Unique card — a fighter's signature technique. Must be paired with a compatible generic card.
/// Base cooldown: 2 turns.
/// 
/// Range System: Defines MIN and MAX hex distance for this attack.
/// Example: MinRange=1, MaxRange=2 means "can hit 1 or 2 hexes away"
/// When paired with a generic card, the generic provides range modifiers.
/// </summary>
public class UniqueCard : CardBase
{
    /// <summary>
    /// Body part tags required for pairing. e.g., ["arm"] means any arm generic can pair.
    /// Empty list means any generic card can pair with this unique.
    /// </summary>
    public List<string> RequiredBodyTags { get; init; } = new();

    /// <summary>
    /// Specific body part(s) that CANNOT pair with this card.
    /// Allows fine-grained restrictions.
    /// </summary>
    public List<BodyPart> ForbiddenBodyParts { get; init; } = new();

    /// <summary>
    /// Optional: requires the opponent to be in a specific damage state for this card to be playable.
    /// e.g., a "Exploit Opening" move that only works if opponent's Torso is Bruised or worse.
    /// </summary>
    public string? RequiresOpponentCondition { get; init; }

    /// <summary>
    /// Minimum range in hexes. Attack can hit opponents at least this far away.
    /// </summary>
    public int MinRange { get; init; } = 1;

    /// <summary>
    /// Maximum range in hexes. Attack cannot hit opponents farther than this.
    /// </summary>
    public int MaxRange { get; init; } = 1;
}
