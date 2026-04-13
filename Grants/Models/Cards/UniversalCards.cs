using Grants.Models.Fighter;

namespace Grants.Models.Cards;

/// <summary>
/// Universal utility cards available to every fighter at all times.
/// These are injected via FighterInstance.GetAllUniques() rather than
/// stored in FighterDefinition, so fighters don't need to declare them individually.
/// </summary>
public static class UniversalCards
{
    /// <summary>
    /// Free Move — pure repositioning. Move up to 7 hexes in any direction.
    /// No bonus power or defense from this card itself; the generic's base stats still apply.
    /// Cooldown 5 makes it a tactical once-per-stretch option.
    /// </summary>
    public static readonly UniqueCard FreeMove = new()
    {
        Id              = "universal_freemove",
        Name            = "Free Move",
        Description     = "Reposition freely up to 7 hexes. No bonus power or defense.",
        RequiredBodyTags = new() { "leg" },
        Keywords        = new(),
        BasePower       = 0,
        BaseDefense     = 0,
        BaseSpeed       = 4,
        MinMovement     = 0,
        MaxMovement     = 7,
        BaseMovementType = MovementType.Free,
        MinRange        = 1,
        MaxRange        = 6,
        PrimaryTarget   = BodyLocation.Torso,
        SecondaryTarget = BodyLocation.Torso,
        BaseCooldown    = 5,
    };

    /// <summary>
    /// Hunker Down — full defensive brace. +10 defense, no movement.
    /// Makes a location nearly impervious for one exchange.
    /// Cooldown 5 prevents spamming.
    /// </summary>
    public static readonly UniqueCard HunkerDown = new()
    {
        Id              = "universal_hunkerdown",
        Name            = "Hunker Down",
        Description     = "Brace for impact. +10 defense, no movement.",
        RequiredBodyTags = new() { "core" },
        Keywords        = new(),
        BasePower       = 0,
        BaseDefense     = 10,
        BaseSpeed       = -1,
        MaxMovement     = 0,
        BaseMovementType = MovementType.None,
        MinRange        = 1,
        MaxRange        = 1,
        PrimaryTarget   = BodyLocation.Torso,
        SecondaryTarget = BodyLocation.Torso,
        BaseCooldown    = 5,
    };

    public static readonly List<UniqueCard> All = new() { FreeMove, HunkerDown };
}
