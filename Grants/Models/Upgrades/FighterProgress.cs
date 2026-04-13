namespace Grants.Models.Upgrades;

/// <summary>
/// Per-player, per-fighter persistent progression data.
/// Tracks wins, card usage, mastery events, and unlocked upgrade slots.
/// Serialized to save file.
/// </summary>
public class FighterProgress
{
    public string FighterId { get; init; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;

    // â”€â”€ Win tracking â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public int TotalWins { get; set; } = 0;
    public int PveWins { get; set; } = 0;
    public int PvpCasualWins { get; set; } = 0;

    // Ranked eligibility
    public bool IsRankedUnlocked => TotalWins >= 15;

    // Elo rating (for ranked PvP)
    public double EloRating { get; set; } = 1200.0;

    // â”€â”€ Card usage tracking â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    /// <summary>Number of distinct matches each card was played in. Key = card ID.</summary>
    public Dictionary<string, int> CardDistinctMatches { get; set; } = new();

    /// <summary>Total landed hits with each card across all matches. Key = card ID.</summary>
    public Dictionary<string, int> CardLandedHits { get; set; } = new();

    /// <summary>Landed hits with each card against a faster opponent. Key = card ID.</summary>
    public Dictionary<string, int> CardLandedVsFaster { get; set; } = new();

    /// <summary>Landed hits with each card from >= 3 hexes away. Key = card ID.</summary>
    public Dictionary<string, int> CardLandedAtRange { get; set; } = new();

    /// <summary>Wins in matches where each card was played at least once. Key = card ID.</summary>
    public Dictionary<string, int> CardWinsWithCard { get; set; } = new();

    /// <summary>Number of times each card delivered a KO blow. Key = card ID.</summary>
    public Dictionary<string, int> CardKillingBlows { get; set; } = new();

    /// <summary>Named event counters for mastery conditions (e.g. "follow_through", "recoil"). Key = counter name.</summary>
    public Dictionary<string, int> EventCounters { get; set; } = new();

    // â”€â”€ Unlocked slots â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    /// <summary>Set of unlocked slot IDs ("cardId:slotIndex").</summary>
    public HashSet<string> UnlockedSlots { get; set; } = new();

    public bool IsSlotUnlocked(string slotId) => UnlockedSlots.Contains(slotId);

    // â”€â”€ Mastery checking â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public bool IsMasteryMet(CardUpgradeSlotDef slot, MasteryCondition mastery)
    {
        return mastery.Type switch
        {
            MasteryConditionType.PlayedInMatches =>
                CardDistinctMatches.GetValueOrDefault(slot.CardId, 0) >= mastery.Target,
            MasteryConditionType.LandedHits =>
                CardLandedHits.GetValueOrDefault(slot.CardId, 0) >= mastery.Target,
            MasteryConditionType.LandedVsFaster =>
                CardLandedVsFaster.GetValueOrDefault(slot.CardId, 0) >= mastery.Target,
            MasteryConditionType.LandedAtRange =>
                CardLandedAtRange.GetValueOrDefault(slot.CardId, 0) >= mastery.Target,
            MasteryConditionType.EventCounter =>
                mastery.CounterKey != null &&
                EventCounters.GetValueOrDefault(mastery.CounterKey, 0) >= mastery.Target,
            MasteryConditionType.WonMatchWithCard =>
                CardWinsWithCard.GetValueOrDefault(slot.CardId, 0) >= mastery.Target,
            MasteryConditionType.WonWithKillingBlow =>
                CardKillingBlows.GetValueOrDefault(slot.CardId, 0) >= mastery.Target,
            _ => false,
        };
    }

    // â”€â”€ Record end-of-match stats â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    /// <summary>Called at match end with a summary of what happened in that match.</summary>
    public void RecordMatchResult(MatchResult result)
    {
        if (result.Won)
        {
            TotalWins++;
            if (result.IsPve) PveWins++;
            if (result.IsCasualPvp) PvpCasualWins++;
        }

        // Card distinct matches: each card played gets +1 if not already counted this match
        foreach (var cardId in result.CardsPlayed)
        {
            CardDistinctMatches[cardId] = CardDistinctMatches.GetValueOrDefault(cardId, 0) + 1;
        }

        // Landing stats
        foreach (var (cardId, count) in result.LandedHitsPerCard)
            CardLandedHits[cardId] = CardLandedHits.GetValueOrDefault(cardId, 0) + count;

        foreach (var (cardId, count) in result.LandedVsFasterPerCard)
            CardLandedVsFaster[cardId] = CardLandedVsFaster.GetValueOrDefault(cardId, 0) + count;

        foreach (var (cardId, count) in result.LandedAtRangePerCard)
            CardLandedAtRange[cardId] = CardLandedAtRange.GetValueOrDefault(cardId, 0) + count;

        // Win-with-card
        if (result.Won)
        {
            foreach (var cardId in result.CardsPlayed)
                CardWinsWithCard[cardId] = CardWinsWithCard.GetValueOrDefault(cardId, 0) + 1;

            if (result.KillingBlowCardId != null)
                CardKillingBlows[result.KillingBlowCardId] =
                    CardKillingBlows.GetValueOrDefault(result.KillingBlowCardId, 0) + 1;
        }

        // Event counters
        foreach (var (key, count) in result.EventCounterDeltas)
            EventCounters[key] = EventCounters.GetValueOrDefault(key, 0) + count;
    }

    public void UnlockSlot(string slotId) => UnlockedSlots.Add(slotId);

    // â”€â”€ Elo â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public void UpdateEloRating(double opponentElo, bool won)
    {
        const double K = 32.0;
        double expected = 1.0 / (1.0 + Math.Pow(10.0, (opponentElo - EloRating) / 400.0));
        double actual = won ? 1.0 : 0.0;
        EloRating = Math.Max(1000.0, EloRating + K * (actual - expected));
    }
}

/// <summary>
/// Summary of one match's events, passed to FighterProgress.RecordMatchResult().
/// Built by UpgradeEngine at match end.
/// </summary>
public class MatchResult
{
    public bool Won { get; init; }
    public bool IsPve { get; init; }
    public bool IsCasualPvp { get; init; }

    /// <summary>Distinct card IDs played in this match (one entry per card, no duplicates).</summary>
    public HashSet<string> CardsPlayed { get; init; } = new();

    /// <summary>How many times each card's attack landed this match.</summary>
    public Dictionary<string, int> LandedHitsPerCard { get; init; } = new();

    /// <summary>Landed hits against a faster opponent per card.</summary>
    public Dictionary<string, int> LandedVsFasterPerCard { get; init; } = new();

    /// <summary>Landed hits at >= 3 hex range per card.</summary>
    public Dictionary<string, int> LandedAtRangePerCard { get; init; } = new();

    /// <summary>Named event counters that incremented this match (e.g. FollowThrough).</summary>
    public Dictionary<string, int> EventCounterDeltas { get; init; } = new();

    /// <summary>Card ID that landed the final KO, if any.</summary>
    public string? KillingBlowCardId { get; init; }
}
