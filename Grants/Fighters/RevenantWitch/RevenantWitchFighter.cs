using Grants.Models.Cards;
using Grants.Models.Fighter;

namespace Grants.Fighters.RevenantWitch;

/// <summary>
/// The Revenant Witch — a spirit-summoner who plants ethereal wisps on the hex board.
///
/// SPIRIT SYSTEM:
///   3 spirits max; 1 starts at the center hex (0,0).
///   Each pre-round, the Witch may place (or recall + redeploy) one spirit on her hex
///   or any adjacent valid hex.
///   End of each round:
///     1. Any spirit occupying the OPPONENT'S exact hex deals 2 random damage steps to them.
///     2. Every spirit then moves 2 hexes toward the opponent (greedy pathfinding).
///
/// UNIQUE CARD INTERACTIONS (applied in OnRoundResolutionStart/Complete):
///   witch_u_soul_swap       — End-of-round: swap positions with the nearest spirit.
///   witch_u_spectral_strike — +1 Power per spirit within 2 hexes of opponent (max +3).
///   witch_u_spirit_ward     — +1 Defense per spirit within 2 hexes of witch (max +3).
///   witch_u_grave_hex       — ArmorBreak; if any spirit is on opponent's exact hex: +2 Power.
///   witch_u_haunting_shroud — +2 Defense if ≥ 2 spirits within 2 hexes of witch.
///   witch_u_soul_drain      — Bleed; consume nearest spirit → +3 Power this round.
///   witch_u_phantom_rush    — Approach 2; spirits travel +2 extra hexes at end of round.
///   witch_u_curse_chain     — Bleed; +2 Power if ≥ 1 spirit within 1 hex of opponent.
///
/// 8 generics / 8 uniques (all CD ≥ 5) / 0 specials.
/// </summary>
public static class RevenantWitchFighter
{
    public const string FighterId = "revenant_witch";

    // ===== GENERIC CARDS =====

    public static readonly GenericCard G_Head = new()
    {
        Id = "witch_g_head",
        Name = "Shadow Gaze",
        Description = "Fixes the opponent with hollow eyes, unsettling their guard.",
        BodyPart = BodyPart.Head,
        SatisfiesTags = new() { "head", "upper" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Torso = new()
    {
        Id = "witch_g_torso",
        Name = "Ethereal Sway",
        Description = "Flowing, ghost-like evasion that bleeds force away.",
        BodyPart = BodyPart.Torso,
        SatisfiesTags = new() { "torso", "upper", "body" },
        BasePower = 2,
        BaseDefense = 2,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftArm = new()
    {
        Id = "witch_g_leftarm",
        Name = "Left Curse Weave",
        Description = "A quick slash that leaves hexed residue across the target.",
        BodyPart = BodyPart.LeftArm,
        SatisfiesTags = new() { "arm", "left", "upper" },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightArm = new()
    {
        Id = "witch_g_rightarm",
        Name = "Right Soul Strike",
        Description = "A heavy downward strike channelling bound spirit energy.",
        BodyPart = BodyPart.RightArm,
        SatisfiesTags = new() { "arm", "right", "upper" },
        BasePower = 3,
        BaseDefense = 1,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Retreat,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftLeg = new()
    {
        Id = "witch_g_leftleg",
        Name = "Phantom Step",
        Description = "A gliding advance along spirit-traced paths.",
        BodyPart = BodyPart.LeftLeg,
        SatisfiesTags = new() { "leg", "left", "lower" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightLeg = new()
    {
        Id = "witch_g_rightleg",
        Name = "Haunted Stride",
        Description = "Driven forward by the weight of the restless dead.",
        BodyPart = BodyPart.RightLeg,
        SatisfiesTags = new() { "leg", "right", "lower" },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Approach,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Core = new()
    {
        Id = "witch_g_core",
        Name = "Ley Line Draw",
        Description = "Centering stance that taps the invisible currents beneath the arena.",
        BodyPart = BodyPart.Core,
        SatisfiesTags = new() { "core", "body" },
        BasePower = 2,
        BaseDefense = 2,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Stance = new()
    {
        Id = "witch_g_stance",
        Name = "Spirit Veil",
        Description = "Drapes the Witch in a shroud of spirit-light, diffusing incoming force.",
        BodyPart = BodyPart.Stance,
        SatisfiesTags = new() { "stance", "lower" },
        BasePower = 1,
        BaseDefense = 2,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    // ===== UNIQUE CARDS =====

    /// <summary>
    /// End-of-round: witch and her nearest spirit swap hex positions.
    /// Pairs with any generic (broad tags).
    /// </summary>
    public static readonly UniqueCard U_SoulSwap = new()
    {
        Id = "witch_u_soul_swap",
        Name = "Soul Swap",
        Description = "Commands the nearest spirit to exchange places with the Witch at round's end.",
        RequiredBodyTags = new() { "upper", "lower" }, // any — match on upper OR lower covers all
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = BodyLocation.Head,
        SecondaryTarget = BodyLocation.Torso,
        BaseCooldown = 3,
    };

    /// <summary>+1 Power per spirit within 2 hexes of the opponent (max +3).</summary>
    public static readonly UniqueCard U_SpectralStrike = new()
    {
        Id = "witch_u_spectral_strike",
        Name = "Spectral Strike",
        Description = "Channels converging spirit energy into a directed blow.",
        RequiredBodyTags = new() { "upper" },
        BasePower = 2,
        BaseDefense = 0,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = BodyLocation.Head,
        SecondaryTarget = BodyLocation.Torso,
        BaseCooldown = 3,
    };

    /// <summary>+1 Defense per spirit within 2 hexes of the Witch (max +3).</summary>
    public static readonly UniqueCard U_SpiritWard = new()
    {
        Id = "witch_u_spirit_ward",
        Name = "Spirit Ward",
        Description = "Nearby spirits coalesce into a shielding lattice around the Witch.",
        RequiredBodyTags = new() { "torso", "body", "upper" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = BodyLocation.Torso,
        SecondaryTarget = BodyLocation.Torso,
        BaseCooldown = 2,
    };

    /// <summary>ArmorBreak. If any spirit is on the opponent's exact hex: +2 Power.</summary>
    public static readonly UniqueCard U_GraveHex = new()
    {
        Id = "witch_u_grave_hex",
        Name = "Grave Hex",
        Description = "Strikes through the spirit already clinging to the opponent, shattering their armor.",
        RequiredBodyTags = new() { "head", "upper" },
        BasePower = 3,
        BaseDefense = 0,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Approach,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = BodyLocation.Head,
        SecondaryTarget = BodyLocation.LeftArm,
        Keywords = new()
        {
            new CardKeywordValue(CardKeyword.ArmorBreak, 1),
        },
        BaseCooldown = 3,
    };

    /// <summary>+2 Defense if ≥ 2 spirits are within 2 hexes of the Witch.</summary>
    public static readonly UniqueCard U_HauntingShroud = new()
    {
        Id = "witch_u_haunting_shroud",
        Name = "Haunting Shroud",
        Description = "Two or more spirits weave a dense shroud; attacks pass through echoes.",
        RequiredBodyTags = new() { "torso", "body" },
        BasePower = 1,
        BaseDefense = 2,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Retreat,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = BodyLocation.Torso,
        SecondaryTarget = BodyLocation.Torso,
        BaseCooldown = 3,
    };

    /// <summary>Bleed. Consume the nearest spirit → +3 Power this round.</summary>
    public static readonly UniqueCard U_SoulDrain = new()
    {
        Id = "witch_u_soul_drain",
        Name = "Soul Drain",
        Description = "Shatters a bound spirit to forge a devastating wound.",
        RequiredBodyTags = new() { "arm", "right", "upper" },
        BasePower = 2,
        BaseDefense = 0,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Retreat,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = BodyLocation.Torso,
        SecondaryTarget = BodyLocation.LeftArm,
        Keywords = new()
        {
            new CardKeywordValue(CardKeyword.Bleed, 1),
        },
        BaseCooldown = 4,
    };

    /// <summary>Approach 2 movement. Spirits move +2 extra hexes at end of round.</summary>
    public static readonly UniqueCard U_PhantomRush = new()
    {
        Id = "witch_u_phantom_rush",
        Name = "Phantom Rush",
        Description = "Surges forward in a wave of spirits, propelling all wisps toward the enemy.",
        RequiredBodyTags = new() { "leg", "lower" },
        BasePower = 2,
        BaseDefense = 0,
        BaseSpeed = 2,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Approach,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = BodyLocation.Torso,
        SecondaryTarget = BodyLocation.RightLeg,
        BaseCooldown = 2,
    };

    /// <summary>Bleed. +2 Power if ≥ 1 spirit is within 1 hex of the opponent.</summary>
    public static readonly UniqueCard U_CurseChain = new()
    {
        Id = "witch_u_curse_chain",
        Name = "Curse Chain",
        Description = "A spirit beside the opponent amplifies the curse into a chain of wounds.",
        RequiredBodyTags = new() { "arm", "left", "upper" },
        BasePower = 3,
        BaseDefense = 0,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Retreat,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = BodyLocation.Torso,
        SecondaryTarget = BodyLocation.Head,
        Keywords = new()
        {
            new CardKeywordValue(CardKeyword.Bleed, 1),
        },
        BaseCooldown = 3,
    };

    // ===== FIGHTER DEFINITION =====

    public static FighterDefinition CreateDefinition() => new()
    {
        Id = FighterId,
        Name = "Revenant Witch",
        Description = "A spirit-controlling fighter who floods the board with spectral wisps that hunt the opponent and empower her strikes.",
        GenericCards = new()
        {
            G_Head, G_Torso, G_LeftArm, G_RightArm,
            G_LeftLeg, G_RightLeg, G_Core, G_Stance,
        },
        UniqueCards = new()
        {
            U_SoulSwap.Clone(), U_SpectralStrike.Clone(), U_SpiritWard.Clone(), U_GraveHex.Clone(),
            U_HauntingShroud.Clone(), U_SoulDrain.Clone(), U_PhantomRush.Clone(), U_CurseChain.Clone(),
        },
        SpecialCards = new(),
        Persona = RevenantWitchPersona.Instance,
        RankedUnlockWins = 15,
    };
}
