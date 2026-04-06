using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;
using Grants.Models.Upgrades;
using System.Text.Json;

namespace Grants.Engine;

/// <summary>
/// Handles applying upgrade tree progress to a FighterInstance before a match,
/// and saving/loading PlayerProfile to disk.
/// </summary>
public static class UpgradeEngine
{
    private static readonly string SaveDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Grants", "Saves");

    /// <summary>
    /// Apply all unlocked upgrades from a FighterProgress to a FighterInstance.
    /// Called at match start to configure the fighter's live stats.
    /// </summary>
    public static void ApplyProgressToInstance(FighterInstance instance, FighterProgress progress, UpgradeTree tree)
    {
        foreach (var nodeId in progress.UnlockedNodes)
        {
            var node = tree.GetNode(nodeId);
            if (node == null) continue;

            switch (node.NodeType)
            {
                case UpgradeNodeType.CardSlot:
                    ApplyCardSlotUpgrade(instance, node);
                    break;
                case UpgradeNodeType.Item:
                    ApplyItemEffect(instance, node.ItemEffect);
                    if (node.ItemId != null)
                        instance.ActiveItemIds.Add(node.ItemId);
                    break;
                case UpgradeNodeType.FinalNode:
                    ApplyFinalNodeEffect(instance, node.FinalEffect);
                    break;
            }
        }
    }

    private static void ApplyCardSlotUpgrade(FighterInstance instance, UpgradeNode node)
    {
        if (node.TargetCardId == null || node.UpgradeEffect == null) return;
        var effect = node.UpgradeEffect;
        string cid = node.TargetCardId;

        switch (effect.UpgradeType)
        {
            case CardUpgradeType.PowerBonus:
                instance.UpgradedCardPower[cid] = (instance.UpgradedCardPower.TryGetValue(cid, out int p) ? p : 0) + effect.StatBonus;
                break;
            case CardUpgradeType.DefenseBonus:
                instance.UpgradedCardDefense[cid] = (instance.UpgradedCardDefense.TryGetValue(cid, out int d) ? d : 0) + effect.StatBonus;
                break;
            case CardUpgradeType.SpeedBonus:
                instance.UpgradedCardSpeed[cid] = (instance.UpgradedCardSpeed.TryGetValue(cid, out int s) ? s : 0) + effect.StatBonus;
                break;
            case CardUpgradeType.MovementBonus:
                instance.UpgradedCardMovement[cid] = (instance.UpgradedCardMovement.TryGetValue(cid, out int m) ? m : 0) + effect.StatBonus;
                break;
            case CardUpgradeType.CooldownReduction:
                instance.UpgradedCardCooldownReduction[cid] = (instance.UpgradedCardCooldownReduction.TryGetValue(cid, out int cr) ? cr : 0) + effect.CooldownReduction;
                break;
            case CardUpgradeType.AddKeyword:
                if (!instance.UpgradedCardKeywords.TryGetValue(cid, out var kws))
                    instance.UpgradedCardKeywords[cid] = kws = new();
                if (!kws.Contains(effect.KeywordAdded))
                    kws.Add(effect.KeywordAdded);
                break;
        }
    }

    private static void ApplyItemEffect(FighterInstance instance, ItemEffect? effect)
    {
        if (effect == null) return;
        if (effect.DamageCapLocation.HasValue && effect.DamageCap.HasValue)
            instance.LocationStates[effect.DamageCapLocation.Value].DamageCap = effect.DamageCap.Value;
    }

    private static void ApplyFinalNodeEffect(FighterInstance instance, FinalNodeEffect? effect)
    {
        if (effect == null) return;
        ApplyItemEffect(instance, effect.Effect);

        if (effect.SpecialCooldownReduction > 0)
        {
            foreach (var special in instance.Definition.SpecialCards)
                instance.UpgradedCardCooldownReduction[special.Id] =
                    (instance.UpgradedCardCooldownReduction.TryGetValue(special.Id, out int cr) ? cr : 0)
                    + effect.SpecialCooldownReduction;
        }
    }

    // --- Save/Load ---

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
