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

    /// <summary>
    /// Minimum hexes this action requires to move (0 = movement is optional).
    /// Sums both cards' MinMovement values.
    /// </summary>
    public int EffectiveMinMovement =>
        Math.Max(0, (Generic?.MinMovement ?? 0) + (Unique?.MinMovement ?? Special?.MinMovement ?? 0));

    /// <summary>
    /// Maximum hexes this action can move (not accounting for per-fighter upgrade bonuses).
    /// Sums both cards' MaxMovement values.
    /// Use FighterInstance.GetCardMovement() when upgrade bonuses matter.
    /// </summary>
    public int EffectiveMaxMovement =>
        (Generic?.MaxMovement ?? 0) + (Unique?.MaxMovement ?? Special?.MaxMovement ?? 0);

    /// <summary>
    /// Movement type for this pair. The unique/special card's type takes priority
    /// (it defines the intent of the move — Approach/Retreat/Free). The generic
    /// card's type is a fallback if the unique/special has None.
    /// </summary>
    public MovementType CombinedMovementType
    {
        get
        {
            var primary = Unique?.BaseMovementType ?? Special?.BaseMovementType ?? MovementType.None;
            if (primary != MovementType.None) return primary;
            return Generic?.BaseMovementType ?? MovementType.None;
        }
    }

    /// <summary>
    /// Minimum range (in hexes) this pair can hit.
    /// For normal pairs: Unique.MinRange + Generic.MinRangeModifier
    /// For standalone specials: Special.MinRange
    /// 
    /// Example:
    /// - Unique: MinRange=1, MaxRange=2
    /// - Generic: MinModifier=0, MaxModifier=+1
    /// - Result MinRange: 1 + 0 = 1
    /// - Result MaxRange: 2 + 1 = 3
    /// - Can hit 1, 2, or 3 hexes away
    /// </summary>
    public int EffectiveMinRange
    {
        get
        {
            int baseMin = Unique?.MinRange ?? Special?.MinRange ?? 1;
            int modifier = Generic?.MinRangeModifier ?? 0;
            return Math.Max(1, baseMin + modifier);  // Minimum is 1 (can't hit 0 hexes away)
        }
    }

    /// <summary>
    /// Maximum range (in hexes) this pair can hit.
    /// For normal pairs: Unique.MaxRange + Generic.MaxRangeModifier
    /// For standalone specials: Special.MaxRange
    /// </summary>
    public int EffectiveMaxRange
    {
        get
        {
            int baseMax = Unique?.MaxRange ?? Special?.MaxRange ?? 1;
            int modifier = Generic?.MaxRangeModifier ?? 0;
            return Math.Max(1, baseMax + modifier);
        }
    }

    /// <summary>
    /// Check if a distance falls within this pair's attack range.
    /// </summary>
    public bool IsInRange(int distance) =>
        distance >= EffectiveMinRange && distance <= EffectiveMaxRange;

    /// <summary>For display purposes: show range as a bracket string.</summary>
    public string RangeDisplay => $"{EffectiveMinRange}-{EffectiveMaxRange}";

    /// <summary>All keywords from both cards combined.</summary>
    public IEnumerable<CardKeywordValue> AllKeywords =>
        (Generic?.Keywords ?? Enumerable.Empty<CardKeywordValue>())
        .Concat(Unique?.Keywords ?? Enumerable.Empty<CardKeywordValue>())
        .Concat(Special?.Keywords ?? Enumerable.Empty<CardKeywordValue>());

    public bool IsValid => Generic != null || (Special != null && Special.Standalone);
}
