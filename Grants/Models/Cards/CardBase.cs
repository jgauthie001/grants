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
/// Base class for all card types. Holds shared stats and upgrade slot tracking.
/// </summary>
public abstract class CardBase
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    // Combat stats (base values — modified by upgrades at runtime via FighterProgress)
    public int BasePower { get; init; }
    public int BaseDefense { get; init; }
    public int BaseSpeed { get; init; }       // -2 to +3
    public int BaseMovement { get; init; }    // Hexes moved when this card is used
    public RangeBracket BaseRange { get; init; } = RangeBracket.Adjacent;

    // Keywords on this card (base set — upgrades may add keywords)
    public List<CardKeyword> Keywords { get; init; } = new();

    // Cooldown (base — upgrades can reduce)
    public int BaseCooldown { get; init; }

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
