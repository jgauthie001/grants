namespace Grants.Models.Cards;

/// <summary>
/// The committed action for one fighter on a given turn.
/// Holds the chosen generic (or null for standalone special) + the chosen unique or special card.
/// </summary>
public class CardPair
{
    /// <summary>The generic card played (body part used). Null if a standalone special is played.</summary>
    public GenericCard? Generic { get; init; }

    /// <summary>
    /// The unique or special card played. One of UniqueCard or SpecialCard will be set.
    /// </summary>
    public UniqueCard? Unique { get; init; }
    public SpecialCard? Special { get; init; }

    /// <summary>Combined speed for this action. Used for resolution ordering.</summary>
    public int CombinedSpeed =>
        (Generic?.BaseSpeed ?? 0) + (Unique?.BaseSpeed ?? Special?.BaseSpeed ?? 0);

    /// <summary>Combined power for this action.</summary>
    public int CombinedPower =>
        (Generic?.BasePower ?? 0) + (Unique?.BasePower ?? Special?.BasePower ?? 0);

    /// <summary>Combined defense for this action.</summary>
    public int CombinedDefense =>
        (Generic?.BaseDefense ?? 0) + (Unique?.BaseDefense ?? Special?.BaseDefense ?? 0);

    /// <summary>Movement hexes this action grants.</summary>
    public int CombinedMovement =>
        (Generic?.BaseMovement ?? 0) + (Unique?.BaseMovement ?? Special?.BaseMovement ?? 0);

    /// <summary>Effective range of the attack portion of this action.</summary>
    public RangeBracket EffectiveRange =>
        Unique?.BaseRange ?? Special?.BaseRange ?? Generic?.BaseRange ?? RangeBracket.Adjacent;

    /// <summary>All keywords from both cards combined.</summary>
    public IEnumerable<CardKeywordValue> AllKeywords =>
        (Generic?.Keywords ?? Enumerable.Empty<CardKeywordValue>())
        .Concat(Unique?.Keywords ?? Enumerable.Empty<CardKeywordValue>())
        .Concat(Special?.Keywords ?? Enumerable.Empty<CardKeywordValue>());

    public bool IsValid => Generic != null || (Special != null && Special.Standalone);
}
