using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;
using Grants.Models.Upgrades;
using System.Text.Json;

namespace Grants.Engine;

/// <summary>
/// Applies upgrade progress to FighterInstances, records end-of-match stats,
/// auto-unlocks newly met slots, and handles save/load.
/// </summary>
public static class UpgradeEngine
{
    private static readonly string SaveDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Grants", "Saves");

    /// <summary>
    /// Apply all unlocked upgrade slots from a FighterProgress to a FighterInstance.
    /// No-op when match.UpgradesEnabled is false â€” safe to call unconditionally.
    /// </summary>
    public static void ApplyProgressToInstance(
        FighterInstance instance,
        FighterProgress progress,
        FighterUpgradeDef upgradeDef,
        bool upgradesEnabled = true)
    {
        if (!upgradesEnabled) return;

        foreach (var slotId in progress.UnlockedSlots)
        {
            var slot = upgradeDef.GetSlot(slotId);
            if (slot == null) continue;
            ApplySlot(instance, slot);
        }
    }

    private static void ApplySlot(FighterInstance instance, CardUpgradeSlotDef slot)
    {
        string cid = slot.CardId;

        switch (slot.UpgradeType)
        {
            case SlotUpgradeType.PowerBonus:
                instance.UpgradedCardPower[cid] = instance.UpgradedCardPower.GetValueOrDefault(cid, 0) + slot.StatBonus;
                break;
            case SlotUpgradeType.DefenseBonus:
                instance.UpgradedCardDefense[cid] = instance.UpgradedCardDefense.GetValueOrDefault(cid, 0) + slot.StatBonus;
                break;
            case SlotUpgradeType.SpeedBonus:
                instance.UpgradedCardSpeed[cid] = instance.UpgradedCardSpeed.GetValueOrDefault(cid, 0) + slot.StatBonus;
                break;
            case SlotUpgradeType.MovementBonus:
                instance.UpgradedCardMovement[cid] = instance.UpgradedCardMovement.GetValueOrDefault(cid, 0) + slot.StatBonus;
                break;
            case SlotUpgradeType.CooldownReduction:
                instance.UpgradedCardCooldownReduction[cid] = instance.UpgradedCardCooldownReduction.GetValueOrDefault(cid, 0) + slot.CooldownReduction;
                break;
            case SlotUpgradeType.RangeExtension:
                instance.UpgradedCardMaxRange[cid] = instance.UpgradedCardMaxRange.GetValueOrDefault(cid, 0) + slot.StatBonus;
                break;
            case SlotUpgradeType.AddKeyword:
                if (!instance.UpgradedCardKeywords.TryGetValue(cid, out var kws))
                    instance.UpgradedCardKeywords[cid] = kws = new();
                // Add or update keyword value
                var existing = kws.FirstOrDefault(k => k.Keyword == slot.KeywordAdded);
                if (existing != null)
                    existing.Value = Math.Max(existing.Value, slot.KeywordValue);
                else
                    kws.Add(new CardKeywordValue(slot.KeywordAdded, slot.KeywordValue));
                break;
            case SlotUpgradeType.PersonaUnlock:
                if (slot.PersonaUnlockId != null)
                    instance.UnlockedPersonaIds.Add(slot.PersonaUnlockId);
                break;
        }
    }

    /// <summary>
    /// Record end-of-match stats and auto-unlock any newly met slots.
    /// Returns the list of newly unlocked slot IDs (for UI display).
    /// </summary>
    public static List<string> RecordMatchAndUnlock(
        FighterProgress progress,
        FighterUpgradeDef upgradeDef,
        MatchResult result)
    {
        progress.RecordMatchResult(result);

        var newlyUnlocked = new List<string>();
        foreach (var slot in upgradeDef.Slots.Values)
        {
            if (!progress.IsSlotUnlocked(slot.SlotId) && upgradeDef.IsSlotAvailable(slot, progress))
            {
                progress.UnlockSlot(slot.SlotId);
                newlyUnlocked.Add(slot.SlotId);
            }
        }
        return newlyUnlocked;
    }

    /// <summary>
    /// Build a MatchResult from a completed MatchState.
    /// Collects cards played by FighterA (the player), landing stats, and event counters.
    /// </summary>
    public static MatchResult BuildMatchResult(
        MatchState match,
        bool playerWon,
        Dictionary<string, int>? eventCounterDeltas = null)
    {
        var cardsPlayed = new HashSet<string>();
        var landedHits = new Dictionary<string, int>();
        var landedVsFaster = new Dictionary<string, int>();
        var landedAtRange = new Dictionary<string, int>();
        string? killingBlow = null;

        foreach (var round in match.History)
        {
            var pair = round.PairA; // FighterA = player
            if (pair == null) continue;

            // Track distinct cards played (generic + unique/special)
            if (pair.Generic != null) cardsPlayed.Add(pair.Generic.Id);
            if (pair.Unique != null) cardsPlayed.Add(pair.Unique.Id);
            if (pair.Special != null) cardsPlayed.Add(pair.Special.Id);

            // Determine attacking card (unique for damage intent)
            string? atkCardId = pair.Unique?.Id ?? pair.Special?.Id;
            if (atkCardId == null) continue;

            bool aLanded = round.DamageToB.Count > 0;
            bool aFaster = round.FighterAFaster;
            int distAtAttack = new Models.Board.HexCoord(match.FighterA.HexQ, match.FighterA.HexR)
                .DistanceTo(new Models.Board.HexCoord(match.FighterB.HexQ, match.FighterB.HexR));

            if (aLanded)
            {
                landedHits[atkCardId] = landedHits.GetValueOrDefault(atkCardId, 0) + 1;

                if (!aFaster) // A landed but was slower = landed vs faster opponent
                    landedVsFaster[atkCardId] = landedVsFaster.GetValueOrDefault(atkCardId, 0) + 1;

                if (distAtAttack >= 3)
                    landedAtRange[atkCardId] = landedAtRange.GetValueOrDefault(atkCardId, 0) + 1;
            }

            // Killing blow: last round where B took damage that ended the match
            if (playerWon && round == match.History[^1] && aLanded)
                killingBlow = atkCardId;
        }

        return new MatchResult
        {
            Won = playerWon,
            IsPve = match.MatchType == Models.Match.MatchType.PvE,
            IsCasualPvp = match.MatchType == Models.Match.MatchType.PvpCasual,
            CardsPlayed = cardsPlayed,
            LandedHitsPerCard = landedHits,
            LandedVsFasterPerCard = landedVsFaster,
            LandedAtRangePerCard = landedAtRange,
            EventCounterDeltas = eventCounterDeltas ?? new(),
            KillingBlowCardId = killingBlow,
        };
    }

    // â”€â”€ Save / Load â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public static void SaveProfile(PlayerProfile profile)
    {
        Directory.CreateDirectory(SaveDir);
        string path = Path.Combine(SaveDir, $"{profile.PlayerId}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static PlayerProfile? LoadProfile(string playerId)
    {
        string path = Path.Combine(SaveDir, $"{playerId}.json");
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<PlayerProfile>(File.ReadAllText(path));
    }

    public static PlayerProfile LoadOrCreateProfile(string playerId, string displayName)
    {
        return LoadProfile(playerId) ?? new PlayerProfile { PlayerId = playerId, DisplayName = displayName };
    }
}
