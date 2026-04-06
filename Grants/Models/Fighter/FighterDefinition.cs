using Grants.Models.Cards;

namespace Grants.Models.Fighter;

/// <summary>
/// Static definition of a fighter — their card pool, base stats, and upgrade tree reference.
/// Shared across all instances of that fighter type.
/// </summary>
public class FighterDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>The 8 generic cards (one per body part).</summary>
    public List<GenericCard> GenericCards { get; init; } = new();

    /// <summary>The 8 unique cards (fighter's signature techniques).</summary>
    public List<UniqueCard> UniqueCards { get; init; } = new();

    /// <summary>The 2 special cards.</summary>
    public List<SpecialCard> SpecialCards { get; init; } = new();

    /// <summary>
    /// Which body location, if Disabled, constitutes a loss for this fighter.
    /// Defaults to Head and Torso (both disabled = KO). Override for special fighters.
    /// </summary>
    public List<BodyLocation> CriticalLocations { get; init; } = new() { BodyLocation.Head, BodyLocation.Torso };

    /// <summary>How many critical locations must be disabled to lose. Default 2.</summary>
    public int KOThreshold { get; init; } = 2;

    /// <summary>Power rating cost table for this fighter's upgrade tree. Set by upgrade tree.</summary>
    public int MaxPowerRating { get; init; } = 0;

    /// <summary>Wins required to unlock ranked PvP with this fighter.</summary>
    public int RankedUnlockWins { get; init; } = 15;

    public IEnumerable<CardBase> AllCards =>
        GenericCards.Cast<CardBase>().Concat(UniqueCards).Concat(SpecialCards);
}
