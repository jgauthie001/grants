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
    public int MinRange { get; set; } = 1;

    /// <summary>
    /// Maximum range in hexes. Attack cannot hit opponents farther than this.
    /// </summary>
    public int MaxRange { get; set; } = 1;

    /// <summary>
    /// Primary target location on the defender. Used when that location is not Disabled.
    /// </summary>
    public Fighter.BodyLocation PrimaryTarget { get; init; } = Fighter.BodyLocation.Torso;

    /// <summary>
    /// Fallback target location if the primary is Disabled on the defender.
    /// </summary>
    public Fighter.BodyLocation SecondaryTarget { get; init; } = Fighter.BodyLocation.Torso;

    /// <summary>Phase in which this card's attack fires. Default: Main.</summary>
    public TurnPhase AttackPhase { get; set; } = TurnPhase.Main;

    /// <summary>Phase in which this card's post-attack repositioning fires. Default: Finish.</summary>
    public TurnPhase PostMovementPhase { get; set; } = TurnPhase.Finish;

    /// <summary>Deep-clones this card. Pass a new ID, or null to keep the same ID.</summary>
    public UniqueCard Clone(string? newId = null) => new UniqueCard
    {
        Id = newId ?? Id,
        Name = Name,
        Description = Description,
        BasePower = BasePower,
        BaseDefense = BaseDefense,
        BaseSpeed = BaseSpeed,
        MinMovement = MinMovement,
        MaxMovement = MaxMovement,
        BaseMovementType = BaseMovementType,
        Keywords = new List<CardKeywordValue>(Keywords.Select(k => new CardKeywordValue(k.Keyword, k.Value))),
        BaseCooldown = BaseCooldown,
        SlotOne = new UpgradeSlot { IsUnlocked = SlotOne.IsUnlocked, UpgradeType = SlotOne.UpgradeType, StatBonus = SlotOne.StatBonus, KeywordAdded = SlotOne.KeywordAdded, CooldownReduction = SlotOne.CooldownReduction },
        SlotTwo = new UpgradeSlot { IsUnlocked = SlotTwo.IsUnlocked, UpgradeType = SlotTwo.UpgradeType, StatBonus = SlotTwo.StatBonus, KeywordAdded = SlotTwo.KeywordAdded, CooldownReduction = SlotTwo.CooldownReduction },
        RequiredBodyTags = new List<string>(RequiredBodyTags),
        ForbiddenBodyParts = new List<BodyPart>(ForbiddenBodyParts),
        RequiresOpponentCondition = RequiresOpponentCondition,
        MinRange = MinRange,
        MaxRange = MaxRange,
        PrimaryTarget = PrimaryTarget,
        SecondaryTarget = SecondaryTarget,
        AttackPhase = AttackPhase,
        PostMovementPhase = PostMovementPhase,
        MovementPhase = MovementPhase,
    };
}
