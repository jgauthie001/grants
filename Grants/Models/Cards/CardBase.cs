namespace Grants.Models.Cards;

/// <summary>
/// Which body part this generic card represents. Determines what unique cards can be paired with it.
/// If the corresponding damage location is Disabled, this card is removed from the available hand.
/// </summary>
public enum BodyPart
{
    Head,
    Torso,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg,
    Core,       // Hips/mid-section — enables grapples, body slams, spins
    Stance,     // Footwork/posture — enables repositioning and setup moves
}

/// <summary>
/// The type of range a card operates in.
/// </summary>
public enum RangeBracket
{
    Adjacent = 1,   // Directly neighboring hex
    Close = 2,      // 2 hexes
    Mid = 3,        // 3 hexes
    Far = 4,        // 4+ hexes
}

/// <summary>
/// The phase of a round in which a card action fires.
/// GenericCard movement defaults to Beginning.
/// UniqueCard/SpecialCard attack defaults to Main; post-attack reposition defaults to Finish.
/// </summary>
public enum TurnPhase
{
    Start,       // Pre-round housekeeping (stage/persona choices, before card selection)
    Beginning,   // First action phase — typically pre-attack movement
    Main,        // Second action phase — typically the attack
    Finish,      // Third action phase — typically post-attack repositioning
    End,         // Post-round housekeeping (cooldowns, bleed, KO check)
}

/// <summary>
/// Defines how the movement on a card is applied relative to the opponent.
/// </summary>
public enum MovementType
{
    /// <summary>Move toward opponent by this many hexes.</summary>
    Approach,
    /// <summary>Move away from opponent by this many hexes.</summary>
    Retreat,
    /// <summary>Move freely in any direction (player chooses destination).</summary>
    Free,
    /// <summary>No repositioning — card is stationary.</summary>
    None,
}

/// <summary>
/// Base class for all card types. Holds shared stats and upgrade slot tracking.
/// </summary>
public abstract class CardBase
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Combat stats (base values — modified by upgrades at runtime via FighterProgress)
    public int BasePower { get; set; }
    public int BaseDefense { get; set; }
    public int BaseSpeed { get; set; }       // -2 to +3
    public int MinMovement { get; set; }     // Minimum hexes required to move (0 = movement is optional)
    public int MaxMovement { get; set; }     // Maximum hexes that can be moved
    public MovementType BaseMovementType { get; set; } = MovementType.None;

    // Keywords on this card (base set — upgrades may add keywords)
    public List<CardKeywordValue> Keywords { get; init; } = new();

    // Cooldown (base — upgrades can reduce)
    public int BaseCooldown { get; set; }

    /// <summary>
    /// Phase in which this card's movement action fires.
    /// GenericCard default: Beginning. UniqueCard/SpecialCard movement uses PostMovementPhase instead.
    /// </summary>
    public TurnPhase MovementPhase { get; set; } = TurnPhase.Beginning;

    // Upgrade slots — each slot holds the chosen upgrade once applied
    public UpgradeSlot SlotOne { get; set; } = new();
    public UpgradeSlot SlotTwo { get; set; } = new();
}

/// <summary>
/// Represents one upgrade slot on a card. Holds a stat boost or keyword addition.
/// </summary>
public class UpgradeSlot
{
    public bool IsUnlocked { get; set; } = false;
    public CardUpgradeType UpgradeType { get; set; } = CardUpgradeType.None;
    public int StatBonus { get; set; } = 0;
    public CardKeyword KeywordAdded { get; set; } = CardKeyword.None;
    public int CooldownReduction { get; set; } = 0;
}

public enum CardUpgradeType
{
    None,
    PowerBonus,
    DefenseBonus,
    SpeedBonus,
    MovementBonus,
    RangeExtension,
    CooldownReduction,
    AddKeyword,
}
