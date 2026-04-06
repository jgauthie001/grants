namespace Grants.Models.Cards;

/// <summary>
/// Unique card — a fighter's signature technique. Must be paired with a compatible generic card.
/// Base cooldown: 2 turns.
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
}
