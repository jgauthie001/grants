namespace Grants.Models.Cards;

/// <summary>
/// Special card — high-impact, fighter-defining. Two per fighter.
/// Base cooldown: 3 turns. Typically slow speed (-1 or -2), high power.
/// May have positioning requirements or prerequisite opponent states.
/// 
/// Range System: Defines MIN and MAX hex distance for this special attack.
/// Example: MinRange=1, MaxRange=3 means "can hit 1, 2, or 3 hexes away"
/// Special cards don't pair with generics (unless Standalone=false), so their range is fixed.
/// </summary>
public class SpecialCard : CardBase
{
    /// <summary>
    /// Minimum range in hexes. Attack can hit opponents at least this far away.
    /// </summary>
    public int MinRange { get; set; } = 1;

    /// <summary>
    /// Maximum range in hexes. Attack cannot hit opponents farther than this.
    /// </summary>
    public int MaxRange { get; set; } = 1;

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
    public SpecialCard Clone(string? newId = null) => new SpecialCard
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
        MinRange = MinRange,
        MaxRange = MaxRange,
        RequiresOpponentLocationDamaged = RequiresOpponentLocationDamaged,
        RequiresOpponentMinState = RequiresOpponentMinState,
        Standalone = Standalone,
        PrimaryTarget = PrimaryTarget,
        SecondaryTarget = SecondaryTarget,
        AttackPhase = AttackPhase,
        PostMovementPhase = PostMovementPhase,
        MovementPhase = MovementPhase,
    };
}
