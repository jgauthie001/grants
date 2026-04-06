namespace Grants.Models.Upgrades;

/// <summary>
/// The complete upgrade tree definition for a fighter.
/// Contains all nodes organized into branches.
/// </summary>
public class UpgradeTree
{
    public string FighterId { get; init; } = string.Empty;

    /// <summary>All nodes in this tree, keyed by node ID.</summary>
    public Dictionary<string, UpgradeNode> Nodes { get; init; } = new();

    /// <summary>Display-ordered branches (branch name → ordered node IDs).</summary>
    public Dictionary<string, List<string>> Branches { get; init; } = new();

    /// <summary>The 4 defining final node IDs.</summary>
    public List<string> FinalNodeIds { get; init; } = new();

    /// <summary>Sum of all node costs — used to set MaxPowerRating on FighterDefinition.</summary>
    public int TotalPowerRating => Nodes.Values.Sum(n => n.PowerRatingValue);

    public UpgradeNode? GetNode(string id) =>
        Nodes.TryGetValue(id, out var node) ? node : null;

    public bool IsAvailable(string nodeId, FighterProgress progress)
    {
        if (!Nodes.TryGetValue(nodeId, out var node)) return false;
        if (progress.UnlockedNodes.Contains(nodeId)) return false; // already unlocked
        return node.Prerequisites.All(prereq => progress.UnlockedNodes.Contains(prereq));
    }
}
