using Grants.Models.Cards;
using Grants.Models.Fighter;

namespace Grants.Fighters.Evolutionary;

/// <summary>
/// The Evolutionary - a fighter who adapts their approach each round by
/// selecting one of five themed evolutions before card selection. Every round
/// they get their Generic + Unique pair PLUS an evolution bonus, making their
/// effective total comparable to standard fighters while offering far more
/// round-to-round decision variance.
///
/// Their individual generics and uniques are slightly below-average, but the
/// guaranteed evolution bonus each round keeps their ceiling competitive.
///
/// 8 generics / 7 uniques / 0 specials / 5 evolutions
/// </summary>
public static class EvolutionaryFighter
{
    public const string FighterId = "evolutionary";

    // ===== GENERIC CARDS =====

    public static readonly GenericCard G_Head = new()
    {
        Id = "evo_g_head",
        Name = "Skull Dart",
        Description = "A darting forward head strike to disrupt rhythm.",
        BodyPart = BodyPart.Head,
        SatisfiesTags = new() { "head", "upper" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Torso = new()
    {
        Id = "evo_g_torso",
        Name = "Core Turn",
        Description = "Rotating the center mass to generate or redirect force.",
        BodyPart = BodyPart.Torso,
        SatisfiesTags = new() { "torso", "upper", "body" },
        BasePower = 2,
        BaseDefense = 2,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftArm = new()
    {
        Id = "evo_g_leftarm",
        Name = "Left Probe",
        Description = "An exploratory jab from the lead hand.",
        BodyPart = BodyPart.LeftArm,
        SatisfiesTags = new() { "arm", "left", "upper" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 2,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Approach,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightArm = new()
    {
        Id = "evo_g_rightarm",
        Name = "Right Drive",
        Description = "A deliberate straight from the rear hand.",
        BodyPart = BodyPart.RightArm,
        SatisfiesTags = new() { "arm", "right", "upper" },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Retreat,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftLeg = new()
    {
        Id = "evo_g_leftleg",
        Name = "Side Step",
        Description = "Lateral repositioning with the lead leg.",
        BodyPart = BodyPart.LeftLeg,
        SatisfiesTags = new() { "leg", "left", "lower" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 1,
        MaxMovement = 2,
        MinMovement = 0,
        BaseMovementType = MovementType.Free,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightLeg = new()
    {
        Id = "evo_g_rightleg",
        Name = "Drive Step",
        Description = "Pushing forward off the rear leg to close or escape.",
        BodyPart = BodyPart.RightLeg,
        SatisfiesTags = new() { "leg", "right", "lower" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 1,
        MaxMovement = 2,
        MinMovement = 0,
        BaseMovementType = MovementType.Approach,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Core = new()
    {
        Id = "evo_g_core",
        Name = "Brace",
        Description = "Tightening the center to absorb punishment.",
        BodyPart = BodyPart.Core,
        SatisfiesTags = new() { "core", "body", "upper", "lower" },
        BasePower = 0,
        BaseDefense = 3,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Retreat,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Stance = new()
    {
        Id = "evo_g_stance",
        Name = "Shift Stance",
        Description = "Adjusting the base to open a new angle.",
        BodyPart = BodyPart.Stance,
        SatisfiesTags = new() { "stance", "lower", "movement" },
        BasePower = 0,
        BaseDefense = 1,
        BaseSpeed = 2,
        BaseMovementType = MovementType.Free,
        MaxMovement = 3,
        BaseCooldown = 1,
    };

    // ===== UNIQUE CARDS =====

    public static readonly UniqueCard U_ProbeStrike = new()
    {
        Id = "evo_u_probe_strike",
        Name = "Probe Strike",
        Description = "A testing blow that reads the opponent's reaction.",
        PrimaryTarget   = BodyLocation.Head,
        SecondaryTarget = BodyLocation.Torso,
        RequiredBodyTags = new() { "upper", "head" },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 2,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        BaseCooldown = 2,
    };

    public static readonly UniqueCard U_IronGuard = new()
    {
        Id = "evo_u_iron_guard",
        Name = "Iron Guard",
        Description = "Locking the arms tight to create a fortress.",
        PrimaryTarget   = BodyLocation.Torso,
        SecondaryTarget = BodyLocation.Core,
        RequiredBodyTags = new() { "body", "upper" },
        BasePower = 1,
        BaseDefense = 4,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Retreat,
        BaseCooldown = 2,
    };

    public static readonly UniqueCard U_QuickSlip = new()
    {
        Id = "evo_u_quick_slip",
        Name = "Quick Slip",
        Description = "Ducking under a blow and resetting with a counter.",
        PrimaryTarget   = BodyLocation.LeftArm,
        SecondaryTarget = BodyLocation.Torso,
        RequiredBodyTags = new() { "upper", "arm" },
        BasePower = 1,
        BaseDefense = 2,
        BaseSpeed = 3,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Free,
        BaseCooldown = 2,
    };

    public static readonly UniqueCard U_GroundShift = new()
    {
        Id = "evo_u_ground_shift",
        Name = "Ground Shift",
        Description = "Drastic footwork to seize a positional advantage.",
        PrimaryTarget   = BodyLocation.LeftLeg,
        SecondaryTarget = BodyLocation.Stance,
        RequiredBodyTags = new() { "lower", "leg" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 2,
        BaseMovementType = MovementType.Approach,
        MinMovement = 0,
        MaxMovement = 3,
        BaseCooldown = 2,
    };

    public static readonly UniqueCard U_PrecisionJab = new()
    {
        Id = "evo_u_precision_jab",
        Name = "Precision Jab",
        Description = "A measured, efficient strike that doesn't overcommit.",
        PrimaryTarget   = BodyLocation.Head,
        SecondaryTarget = BodyLocation.RightArm,
        RequiredBodyTags = new() { "upper", "head", "arm" },
        BasePower = 2,
        BaseDefense = 2,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        BaseCooldown = 2,
    };

    public static readonly UniqueCard U_CoreBurst = new()
    {
        Id = "evo_u_core_burst",
        Name = "Core Burst",
        Description = "An explosive rotation driving force from the center.",
        PrimaryTarget   = BodyLocation.Torso,
        SecondaryTarget = BodyLocation.Core,
        RequiredBodyTags = new() { "body", "core", "upper" },
        BasePower = 3,
        BaseDefense = 1,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Retreat,
        BaseCooldown = 3,
    };

    public static readonly UniqueCard U_SweepingArc = new()
    {
        Id = "evo_u_sweeping_arc",
        Name = "Sweeping Arc",
        Description = "Wide looping attack that covers multiple angles.",
        PrimaryTarget   = BodyLocation.RightLeg,
        SecondaryTarget = BodyLocation.Stance,
        RequiredBodyTags = new() { "lower", "leg", "stance" },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 0,
        Keywords = new()
        {
            new Models.Cards.CardKeywordValue(Models.Cards.CardKeyword.Knockback),
        },
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Retreat,
        BaseCooldown = 3,
    };

    // ===== EVOLUTION CARDS =====
    // Chosen once per round before card selection; their stats are added as round
    // modifiers (not played as a card pair slot). No cooldown - all five available
    // every round. Stats are intentionally modest; the choice is strategic.

    public static readonly SpecialCard E_IronShell = new()
    {
        Id = "evo_e_iron_shell",
        Name = "Iron Shell",
        Description = "Harden the body - all actions feel the weight of your armor.",
        BasePower = 0,
        BaseDefense = 3,
        BaseSpeed = 0,
    };

    public static readonly SpecialCard E_HuntersEdge = new()
    {
        Id = "evo_e_hunters_edge",
        Name = "Hunter's Edge",
        Description = "Channel aggression - raw power flows through every action.",
        BasePower = 3,
        BaseDefense = 0,
        BaseSpeed = 0,
    };

    public static readonly SpecialCard E_PredatoryBurst = new()
    {
        Id = "evo_e_predatory_burst",
        Name = "Predatory Burst",
        Description = "Explosive speed and vicious intent combine in a sudden surge.",
        BasePower = 2,
        BaseDefense = 0,
        BaseSpeed = 2,
    };

    public static readonly SpecialCard E_AdaptiveBalance = new()
    {
        Id = "evo_e_adaptive_balance",
        Name = "Adaptive Balance",
        Description = "Perfect equilibrium - powerful and protected in equal measure.",
        BasePower = 1,
        BaseDefense = 2,
        BaseSpeed = 1,
    };

    public static readonly SpecialCard E_VenomStrike = new()
    {
        Id = "evo_e_venom_strike",
        Name = "Venom Strike",
        Description = "Coating each action in persistent, festering pressure.",
        BasePower = 2,
        BaseDefense = 0,
        BaseSpeed = 1,
        Keywords = new()
        {
            new Models.Cards.CardKeywordValue(Models.Cards.CardKeyword.Bleed, 1),
        },
    };

    // ===== FIGHTER DEFINITION =====

    public static FighterDefinition CreateDefinition() => new()
    {
        Id          = FighterId,
        Name        = "The Evolutionary",
        Description = "Picks one of five evolutions before every round, shaping their combat style to the moment. Individually modest cards, but the evolution bonus makes every approach viable.",
        Persona     = EvolutionaryPersona.Instance,
        GenericCards = new()
        {
            G_Head, G_Torso, G_LeftArm, G_RightArm,
            G_LeftLeg, G_RightLeg, G_Core, G_Stance,
        },
        UniqueCards = new()
        {
            U_ProbeStrike, U_IronGuard, U_QuickSlip, U_GroundShift,
            U_PrecisionJab, U_CoreBurst, U_SweepingArc,
        },
        SpecialCards = new(),
        EvolutionCards = new()
        {
            E_IronShell, E_HuntersEdge, E_PredatoryBurst,
            E_AdaptiveBalance, E_VenomStrike,
        },
        CriticalLocations = new() { BodyLocation.Head, BodyLocation.Torso },
        KOThreshold = 2,
    };
}
