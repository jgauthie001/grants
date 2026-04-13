namespace Grants.Models.Upgrades;

/// <summary>
/// Defines all upgrade slots for one fighter.
/// Each card has up to 3 slots, authored once per fighter.
/// </summary>
public class FighterUpgradeDef
{
    public string FighterId { get; init; } = string.Empty;

    /// <summary>All slot definitions: keyed by SlotId (cardId:slotIndex).</summary>
    public Dictionary<string, CardUpgradeSlotDef> Slots { get; init; } = new();

    public CardUpgradeSlotDef? GetSlot(string slotId) =>
        Slots.TryGetValue(slotId, out var s) ? s : null;

    /// <summary>All slots for a specific card, ordered by SlotIndex.</summary>
    public List<CardUpgradeSlotDef> GetCardSlots(string cardId) =>
        Slots.Values
             .Where(s => s.CardId == cardId)
             .OrderBy(s => s.SlotIndex)
             .ToList();

    /// <summary>
    /// Check if a specific slot can be unlocked given the current progress.
    /// </summary>
    public bool IsSlotAvailable(CardUpgradeSlotDef slot, FighterProgress progress)
    {
        if (progress.IsSlotUnlocked(slot.SlotId)) return false;

        if (slot.Mastery != null)
        {
            // Slot 3: check mastery condition
            return progress.IsMasteryMet(slot, slot.Mastery);
        }
        else
        {
            // Slots 1 & 2: distinct matches + breadth
            int played = progress.CardDistinctMatches.GetValueOrDefault(slot.CardId, 0);
            return played >= slot.DistinctMatchesRequired;
        }
    }
}
