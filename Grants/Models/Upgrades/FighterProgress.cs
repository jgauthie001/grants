namespace Grants.Models.Upgrades;

/// <summary>
/// Per-player, per-fighter persistent progression data.
/// Tracks wins, upgrade points, unlocked nodes, and ranked eligibility.
/// Serialized to save file.
/// </summary>
public class FighterProgress
{
    public string FighterId { get; init; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;

    // Win tracking
    public int TotalWins { get; set; } = 0;
    public int PveWins { get; set; } = 0;
    public int PvpCasualWins { get; set; } = 0;
    // Ranked wins don't count toward upgrades

    // Upgrade points (only PVE + casual pvp wins contribute)
    public int UpgradePoints { get; set; } = 0;
    public int SpentPoints { get; set; } = 0;
    public int AvailablePoints => UpgradePoints - SpentPoints;

    // Unlocked upgrade nodes
    public HashSet<string> UnlockedNodes { get; set; } = new();

    // Power rating (sum of unlocked node PowerRatingValues)
    public int PowerRating { get; set; } = 0;

    // Ranked eligibility
    public bool IsRankedUnlocked => TotalWins >= 15;

    /// <summary>
    /// Calculates upgrade points earned for a given win count.
    /// Front-loaded: more points per win early on.
    /// Wins 1-20: 1 pt/win. Wins 21-50: 1 pt/2 wins. Wins 51-100: 1 pt/3 wins.
    /// </summary>
    public static int CalculateTotalUpgradePoints(int wins)
    {
        int points = 0;
        points += Math.Min(wins, 20);                         // wins 1-20
        if (wins > 20) points += (Math.Min(wins, 50) - 20) / 2;  // wins 21-50
        if (wins > 50) points += (Math.Min(wins, 100) - 50) / 3; // wins 51-100
        if (wins > 100) points += (wins - 100) / 5;          // wins 100+: trickle
        return points;
    }

    /// <summary>Record a win of the given type and recalculate upgrade points.</summary>
    public void RecordWin(bool isPve, bool isCasualPvp)
    {
        TotalWins++;
        if (isPve) PveWins++;
        if (isCasualPvp) PvpCasualWins++;

        int eligibleWins = PveWins + PvpCasualWins;
        UpgradePoints = CalculateTotalUpgradePoints(eligibleWins);
    }

    public bool TryUnlockNode(UpgradeNode node)
    {
        if (AvailablePoints < node.Cost) return false;
        SpentPoints += node.Cost;
        PowerRating += node.PowerRatingValue;
        UnlockedNodes.Add(node.Id);
        return true;
    }

    public bool HasItem(string itemId) =>
        UnlockedNodes.Any(nid => nid == itemId);
}
