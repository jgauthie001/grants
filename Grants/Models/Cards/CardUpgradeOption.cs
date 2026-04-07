using Grants.Models.Cards;

namespace Grants.Models.Upgrades;

/// <summary>
/// Defines one available upgrade option for a card.
/// Each card in a fighter can have up to 2 upgrade options that players can choose from
/// when they spend progression points.
/// </summary>
public class CardUpgradeOption
{
    /// <summary>The card this upgrade applies to.</summary>
    public string CardId { get; set; } = string.Empty;

    /// <summary>The type of upgrade (PowerBonus, CooldownReduction, AddKeyword, etc).</summary>
    public CardUpgradeType Type { get; set; }

    /// <summary>Numeric bonus for stat upgrades (Power +2, Defense +1, Speed +1, Movement +1, Cooldown -1).</summary>
    public int StatBonus { get; set; } = 0;

    /// <summary>Keyword to add for AddKeyword type upgrades.</summary>
    public CardKeyword KeywordToAdd { get; set; } = CardKeyword.None;

    /// <summary>Optional keyword value for stacking keywords like Bleed.</summary>
    public int KeywordValue { get; set; } = 1;

    /// <summary>Human-readable description of this upgrade option.</summary>
    public string Description { get; set; } = string.Empty;

    public CardUpgradeOption() { }

    public CardUpgradeOption(string cardId, CardUpgradeType type, int statBonus, string description)
    {
        CardId = cardId;
        Type = type;
        StatBonus = statBonus;
        Description = description;
    }

    public CardUpgradeOption(string cardId, CardKeyword keyword, int keywordValue, string description)
    {
        CardId = cardId;
        Type = CardUpgradeType.AddKeyword;
        KeywordToAdd = keyword;
        KeywordValue = keywordValue;
        Description = description;
    }

    public override string ToString()
    {
        if (Type == CardUpgradeType.AddKeyword)
            return $"{Description}";
        
        return $"{Description}";
    }
}
