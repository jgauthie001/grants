using Grants.Models.Cards;
using Grants.Models.Fighter;

namespace Grants.Fighters.Cursed;

/// <summary>
/// "The Catalyst" — an alternate Cursed fighter using CatalystPersona.
/// Shares the same card pool as CursedFighter; persona differences only.
///
/// Key gameplay change vs The Cursed:
///   - Opponent tokens give a BUFF (+1 Pwr/+1 Spd) when spent, not a penalty.
///   - The Catalyst can spend their own pool tokens before each round for +2 Pwr/+2 Spd.
/// </summary>
public static class CatalystFighter
{
    public const string FighterId = "catalyst";

    public static FighterDefinition CreateDefinition() => new()
    {
        Id = FighterId,
        Name = "The Catalyst",
        Description = "A cursed fighter who empowers whoever spends their tokens. " +
                      "Opponents are tempted to gather curse tokens for buffs, " +
                      "but The Catalyst spends twice as hard from their own pool.",
        Persona = CatalystPersona.Instance,
        GenericCards = new()
        {
            CursedFighter.G_Head.Clone(),
            CursedFighter.G_Torso.Clone(),
            CursedFighter.G_LeftArm.Clone(),
            CursedFighter.G_RightArm.Clone(),
            CursedFighter.G_LeftLeg.Clone(),
            CursedFighter.G_RightLeg.Clone(),
            CursedFighter.G_Core.Clone(),
            CursedFighter.G_Stance.Clone(),
        },
        UniqueCards = new()
        {
            CursedFighter.U_WretchedStrike.Clone(),
            CursedFighter.U_PhantomPull.Clone(),
            CursedFighter.U_DarkEmpowerment.Clone(),
            CursedFighter.U_CurseLash.Clone(),
            CursedFighter.U_HexBarrage.Clone(),
            CursedFighter.U_MarkOfDoom.Clone(),
        },
        SpecialCards = new()
        {
            CursedFighter.S_CursedBinding.Clone(),
            CursedFighter.S_CurseUnleashed.Clone(),
        },
        CriticalLocations = new() { Models.Fighter.BodyLocation.Head, Models.Fighter.BodyLocation.Torso },
        KOThreshold = 2,
        RankedUnlockWins = 20,
    };
}
