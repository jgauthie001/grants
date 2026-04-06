namespace Grants.Models.Match;

/// <summary>
/// Persistent player profile saved to disk. Tracks progress across all fighters.
/// </summary>
public class PlayerProfile
{
    public string PlayerId { get; set; } = Guid.NewGuid().ToString();
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Per-fighter progress. Key = fighter ID.</summary>
    public Dictionary<string, Grants.Models.Upgrades.FighterProgress> FighterProgress { get; set; } = new();

    /// <summary>Match history summary (last 100).</summary>
    public List<MatchRecord> RecentMatches { get; set; } = new();

    /// <summary>PvP matchmaking rating (overall, not per fighter).</summary>
    public int MatchmakingRating { get; set; } = 1000;

    public Upgrades.FighterProgress GetOrCreateProgress(string fighterId)
    {
        if (!FighterProgress.TryGetValue(fighterId, out var prog))
        {
            prog = new Upgrades.FighterProgress { FighterId = fighterId, PlayerId = PlayerId };
            FighterProgress[fighterId] = prog;
        }
        return prog;
    }
}

/// <summary>A brief record of a completed match for history display.</summary>
public class MatchRecord
{
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
    public MatchType MatchType { get; set; }
    public string FighterId { get; set; } = string.Empty;
    public string OpponentName { get; set; } = string.Empty;
    public bool Won { get; set; }
    public int Rounds { get; set; }
}
