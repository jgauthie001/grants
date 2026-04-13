using Grants.Models.Cards;

namespace Grants.Models.Upgrades;

/// <summary>
/// Types of upgrade that can be applied to a card slot.
/// </summary>
public enum SlotUpgradeType
{
    PowerBonus,
    DefenseBonus,
    SpeedBonus,
    MovementBonus,
    CooldownReduction,
    RangeExtension,
    AddKeyword,
    PersonaUnlock,  // Grants a named string flag personas can query
}

/// <summary>
/// The mastery condition type that governs when Slot 3 unlocks.
/// </summary>
public enum MasteryConditionType
{
    /// <summary>Card played in N distinct matches.</summary>
    PlayedInMatches,
    /// <summary>Attack with this card landed N times (any match).</summary>
    LandedHits,
    /// <summary>Attack with this card landed against a faster opponent N times.</summary>
    LandedVsFaster,
    /// <summary>Attack with this card landed from >= MinDistance hexes N times.</summary>
    LandedAtRange,
    /// <summary>A specific named event counter reaches N (e.g. FollowThrough, Recoil).</summary>
    EventCounter,
    /// <summary>Won a match in which this card was played at least once.</summary>
    WonMatchWithCard,
    /// <summary>Won a match in which this card was the killing blow.</summary>
    WonWithKillingBlow,
}

/// <summary>
/// Describes the mastery condition that must be met to unlock Slot 3.
/// </summary>
public class MasteryCondition
{
    public MasteryConditionType Type { get; init; }

    /// <summary>Target count to reach (e.g. 8 landed hits).</summary>
    public int Target { get; init; } = 1;

    /// <summary>For LandedAtRange: required minimum distance to count.</summary>
    public int MinDistance { get; init; } = 0;

    /// <summary>For EventCounter: the named counter key in FighterProgress.</summary>
    public string? CounterKey { get; init; }

    /// <summary>Human-readable description shown in the UI.</summary>
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// One unlock slot definition for a specific card.
/// Cards have 3 slots each: Slot 1 (easy breadth), Slot 2 (moderate), Slot 3 (mastery).
/// </summary>
public class CardUpgradeSlotDef
{
    /// <summary>The card this slot belongs to (by card Id).</summary>
    public string CardId { get; init; } = string.Empty;

    /// <summary>0 = Slot 1, 1 = Slot 2, 2 = Slot 3.</summary>
    public int SlotIndex { get; init; }

    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    // --- What this slot unlocks ---
    public SlotUpgradeType UpgradeType { get; init; }
    public int StatBonus { get; init; } = 0;
    public int CooldownReduction { get; init; } = 0;
    public CardKeyword KeywordAdded { get; init; } = CardKeyword.None;
    public int KeywordValue { get; init; } = 1;
    public string? PersonaUnlockId { get; init; }

    // --- Unlock gate ---
    // Slots 1 and 2 use DistinctMatchesRequired + breadth check.
    // Slot 3 uses MasteryCondition.
    public int DistinctMatchesRequired { get; init; } = 0;   // For slots 1 & 2
    public int BreadthRequired { get; init; } = 0;           // Minimum distinct cards played in that match
    public MasteryCondition? Mastery { get; init; }          // Slot 3 only

    /// <summary>Unique slot identifier: cardId + ":" + slotIndex.</summary>
    public string SlotId => $"{CardId}:{SlotIndex}";
}

