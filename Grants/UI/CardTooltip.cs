using Grants.Models.Cards;

namespace Grants.UI;

/// <summary>
/// Provides tooltip information for cards: descriptions of stats, keywords, and abilities.
/// </summary>
public static class CardTooltip
{
    /// <summary>Get detailed description of a keyword.</summary>
    public static string GetKeywordDescription(CardKeyword keyword) => keyword switch
    {
        CardKeyword.None => "",

        // Damage modifiers
        CardKeyword.Bleed => "Bleed: Target location takes +1 damage on your next turn (stacks)",
        CardKeyword.ArmorBreak => "Armor Break: Reduces opponent's defense by 1 this turn",
        CardKeyword.Piercing => "Piercing: Ignores half of opponent's defense",
        CardKeyword.Crushing => "Crushing: Advances damage state by 1 extra step",

        // Speed/timing
        CardKeyword.Feint => "Feint: Forces opponent to reveal speed before they commit",
        CardKeyword.Quickstep => "Quickstep: Adds +1 to your combined speed this turn",
        CardKeyword.Lunge => "Lunge: +1 attack range this turn, but -1 defense",

        // Control
        CardKeyword.Stagger => "Stagger: On hit, increases opponent's cooldowns by +1 next turn",
        CardKeyword.Disrupt => "Disrupt: On hit, cancels opponent's unique card (generic only)",
        CardKeyword.Knockback => "Knockback: On hit, pushes opponent 1 hex away",

        // Defensive
        CardKeyword.Guard => "Guard: +2 defense this turn, but cannot attack",
        CardKeyword.Parry => "Parry: If opponent hits same body part, counter at +1 power",
        CardKeyword.Deflect => "Deflect: On being hit, redirects half damage to random location",
        CardKeyword.MaxDamageCap => "Max Damage Cap: Caps how far one hit can advance this location (see card value)",

        // Positional
        CardKeyword.Sidestep => "Sidestep: Allows diagonal movement (ignores normal rules)",
        CardKeyword.Press => "Press: After landing hit, move 1 hex toward opponent free",
        CardKeyword.Retreat => "Retreat: Your movement cannot be prevented this turn",

        // Debug
        CardKeyword.Kill => "Kill [TEST]: Instantly defeats opponent (testing only)",

        // Curse keywords
        CardKeyword.CurseGain => "Curse Gain: On hit, gain 1 extra Curse token to The Cursed's pool",
        CardKeyword.CursePull => "Curse Pull: On hit, pull opponent N hexes (N = their Curse tokens)",
        CardKeyword.CurseEmpower => "Curse Empower: +N power this attack (N = The Cursed's pool)",
        CardKeyword.CurseWeaken => "Curse Weaken: -N opponent defense this attack (N = their Curse tokens)",

        // Chivalrous keywords
        CardKeyword.Pull => "Pull: On hit, pull opponent 1 hex toward you",
        CardKeyword.ChivalryBonus => "Chivalry Bonus: +N damage steps if opponent holds chivalry tokens",
        CardKeyword.DistanceGuard => "Distance Guard: +2 defense if opponent is 3+ hexes away",

        _ => "Unknown keyword"
    };

    /// <summary>Get descriptions for card stats.</summary>
    public static List<string> GetCardStatDescriptions(CardBase card) => new()
    {
        $"Power: {card.BasePower:+#;-#;0}",
        $"Defense: {card.BaseDefense:+#;-#;0}",
        $"Speed: {card.BaseSpeed:+#;-#;0}",
        $"Movement: {GetMovementDisplay(card)}",
        $"Range: {GetRangeDisplay(card)}",
        $"Cooldown: {card.BaseCooldown} turn" + (card.BaseCooldown != 1 ? "s" : ""),
    };

    /// <summary>Get movement display for this card type.</summary>
    private static string GetMovementDisplay(CardBase card)
    {
        if (card.MaxMovement == 0) return "None";
        string type = card.BaseMovementType switch
        {
            MovementType.Approach => ">",
            MovementType.Retreat  => "<",
            MovementType.Free     => "*",
            _                     => "-",
        };
        return card.MinMovement == card.MaxMovement
            ? $"{type}{card.MaxMovement} hex"
            : $"{type}{card.MinMovement}-{card.MaxMovement} hex";
    }

    /// <summary>Get range display for this card type.</summary>
    private static string GetRangeDisplay(CardBase card) => card switch
    {
        UniqueCard unique => $"{unique.MinRange}-{unique.MaxRange}",
        SpecialCard special => $"{special.MinRange}-{special.MaxRange}",
        GenericCard => "Modifier",  // Generic cards don't have inherent range
        _ => "?"
    };

    /// <summary>Build full tooltip content for a card.</summary>
    public static List<string> GetCardTooltip(CardBase card)
    {
        var lines = new List<string>();

        // Card header
        lines.Add(card.Name);
        lines.Add("---" + new string('-', Math.Max(0, card.Name.Length - 3)));
        lines.Add("");

        // Card description
        if (!string.IsNullOrEmpty(card.Description))
        {
            lines.AddRange(WrapText(card.Description, 50));
            lines.Add("");
        }

        // Stats
        lines.AddRange(GetCardStatDescriptions(card));
        lines.Add("");

        // Keywords
        if (card.Keywords.Count > 0)
        {
            lines.Add("KEYWORDS:");
            foreach (var kw in card.Keywords)
            {
                // MaxDamageCap: include the cap level in the description
                string desc = kw.Keyword == CardKeyword.MaxDamageCap
                    ? $"Max Damage Cap: Hit cannot advance this location past {(kw.Value == 1 ? "Bruised" : kw.Value == 2 ? "Injured" : $"level {kw.Value}")}"
                    : GetKeywordDescription(kw.Keyword);
                if (!string.IsNullOrEmpty(desc))
                    lines.AddRange(WrapText(desc, 50));
            }
        }

        return lines;
    }

    /// <summary>Wrap text to specified width (approximate for monospace fonts).</summary>
    private static List<string> WrapText(string text, int maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = "";

        foreach (var word in words)
        {
            if ((currentLine + " " + word).Length <= maxWidth)
            {
                currentLine += (currentLine.Length > 0 ? " " : "") + word;
            }
            else
            {
                if (!string.IsNullOrEmpty(currentLine))
                    lines.Add(currentLine);
                currentLine = word;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        return lines;
    }
}
