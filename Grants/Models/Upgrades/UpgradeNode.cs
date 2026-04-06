using Grants.Models.Cards;
using Grants.Models.Fighter;

namespace Grants.Models.Upgrades;

public enum UpgradeNodeType
{
    /// <summary>Upgrades one stat slot on a specific card.</summary>
    CardSlot,

    /// <summary>A passive item effect (not tied to a card).</summary>
    Item,

    /// <summary>One of the 4 defining final nodes. Unlocked last, gated by branch completion.</summary>
    FinalNode,
}

/// <summary>
/// A single node in a fighter's upgrade tree.
/// </summary>
public class UpgradeNode
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public UpgradeNodeType NodeType { get; init; }

    /// <summary>Upgrade point cost to unlock this node.</summary>
    public int Cost { get; init; } = 1;

    /// <summary>Power rating contribution (for PvP matchmaking).</summary>
    public int PowerRatingValue { get; init; } = 1;

    /// <summary>IDs of nodes that must be unlocked before this one is available.</summary>
    public List<string> Prerequisites { get; init; } = new();

    // --- For CardSlot nodes ---
    /// <summary>Which card this node upgrades (by card Id).</summary>
    public string? TargetCardId { get; init; }
    public int SlotIndex { get; init; } = 0; // 0 = slot one, 1 = slot two

    /// <summary>The upgrade to apply to the card slot.</summary>
    public UpgradeSlot? UpgradeEffect { get; init; }

    // --- For Item nodes ---
    /// <summary>Unique item id. Applied to FighterInstance.ActiveItemIds when unlocked.</summary>
    public string? ItemId { get; init; }
    public ItemEffect? ItemEffect { get; init; }

    // --- For FinalNode ---
    /// <summary>Description of the defining passive effect.</summary>
    public FinalNodeEffect? FinalEffect { get; init; }

    /// <summary>Which branch (by name) this node belongs to — for tree layout.</summary>
    public string Branch { get; init; } = string.Empty;
}

/// <summary>Passive item effects that apply during a match if the item is unlocked.</summary>
public class ItemEffect
{
    /// <summary>If set, this body location cannot exceed this damage state.</summary>
    public BodyLocation? DamageCapLocation { get; init; }
    public DamageState? DamageCap { get; init; }

    /// <summary>Flat power bonus to all cards that use a specific body tag.</summary>
    public string? BodyTagPowerBonus { get; init; }
    public int BodyTagPowerValue { get; init; }

    /// <summary>Speed modifier on all cards of a specific body tag.</summary>
    public string? BodyTagSpeedBonus { get; init; }
    public int BodyTagSpeedValue { get; init; }

    /// <summary>Triggered effect: on first Injured location per match, apply a temporary speed boost.</summary>
    public bool AdrenalineOnFirstInjury { get; init; }
    public int AdrenalineSpeedBonus { get; init; }
    public int AdrenalineTurns { get; init; }
}

/// <summary>Defines a final node's passive/structural effect.</summary>
public class FinalNodeEffect
{
    /// <summary>Free-text description used in UI. Logic is applied via FighterInstance item system.</summary>
    public string FlavorDescription { get; init; } = string.Empty;

    /// <summary>Item-level effect this final node grants.</summary>
    public ItemEffect? Effect { get; init; }

    /// <summary>Cooldown reduction to apply to specials.</summary>
    public int SpecialCooldownReduction { get; init; } = 0;

    /// <summary>After winning a speed duel (faster hit lands), refund cooldown on generics.</summary>
    public bool MovementCooldownRefundOnSpeedWin { get; init; } = false;

    /// <summary>On simultaneous hit that YOU win power on: apply half damage to a second location.</summary>
    public bool SplashDamageOnSimultaneousPowerWin { get; init; } = false;
}
