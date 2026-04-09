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

    /// <summary>
    /// Effective range of the attack portion of this action.
    /// For normal pairs: UniqueCard.BaseRange + GenericCard.RangeModifier
    /// For standalone specials: SpecialCard.BaseRange
    /// 
    /// Range values: Adjacent=1, Close=2, Mid=3, Far=4
    /// Modifiers can shift range up/down (e.g., +1 for extended reach, -1 for short range).
    /// Final range is clamped to valid brackets.
    /// </summary>
    public int EffectiveRangeValue
    {
        get
        {
            int baseRange = (Unique?.BaseRange ?? Special?.BaseRange ?? RangeBracket.Adjacent) switch
            {
                RangeBracket.Adjacent => 1,
                RangeBracket.Close => 2,
                RangeBracket.Mid => 3,
                RangeBracket.Far => 4,
                _ => 1
            };

            int modifier = Generic?.RangeModifier ?? 0;
            int effectiveRange = baseRange + modifier;

            // Clamp to valid range (minimum Adjacent=1, practical maximum Far=4 but allow higher)
            return Math.Max(1, effectiveRange);
        }
    }

    /// <summary>
    /// Effective range as a RangeBracket enum.
    /// Used for display and legacy compatibility.
    /// </summary>
    public RangeBracket EffectiveRange =>
        EffectiveRangeValue switch
        {
            1 => RangeBracket.Adjacent,
            2 => RangeBracket.Close,
            3 => RangeBracket.Mid,
            4 or _ => RangeBracket.Far  // 4+ all map to Far
        };

    /// <summary>Combined range for display. For UI showing range value.</summary>
    public int CombinedRange => EffectiveRangeValue;
    public IEnumerable<CardKeywordValue> AllKeywords =>
        (Generic?.Keywords ?? Enumerable.Empty<CardKeywordValue>())
        .Concat(Unique?.Keywords ?? Enumerable.Empty<CardKeywordValue>())
        .Concat(Special?.Keywords ?? Enumerable.Empty<CardKeywordValue>());

    public bool IsValid => Generic != null || (Special != null && Special.Standalone);
}
