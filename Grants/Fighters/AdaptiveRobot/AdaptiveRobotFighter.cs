using Grants.Models.Cards;
using Grants.Models.Fighter;

namespace Grants.Fighters.AdaptiveRobot;

/// <summary>
/// The Adaptive Robot — a mechanical combat unit that manages a bank of energy tokens
/// and cycles through 5 operational modes each round. Its 12 unique cards (all CD≥5)
/// each spend energy tokens on use; many gain bonus effects when the active mode matches
/// their function. Starting with 3 energy (max 5), the robot must pace its output: empty
/// energy triggers a -2 Power / -2 Speed penalty, while overflow above 5 deals 2 random
/// damage steps to itself.
/// </summary>
public static class AdaptiveRobotFighter
{
    public const string FighterId = "adaptive_robot";

    // ===== GENERIC CARDS =====

    public static readonly GenericCard G_Head = new()
    {
        Id = "bot_g_head",
        Name = "Scan Array",
        Description = "Optical sensors sweep for targeting data and defensive read.",
        BodyPart = BodyPart.Head,
        SatisfiesTags = new() { "head", "upper" },
        BasePower = 1,
        BaseDefense = 2,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Torso = new()
    {
        Id = "bot_g_torso",
        Name = "Chassis Brace",
        Description = "Reinforcing the central chassis to absorb and redirect force.",
        BodyPart = BodyPart.Torso,
        SatisfiesTags = new() { "torso", "upper", "body" },
        BasePower = 2,
        BaseDefense = 3,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftArm = new()
    {
        Id = "bot_g_leftarm",
        Name = "Left Actuator Strike",
        Description = "Hydraulic actuator delivers a fast probing strike.",
        BodyPart = BodyPart.LeftArm,
        SatisfiesTags = new() { "arm", "left", "upper" },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Retreat,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightArm = new()
    {
        Id = "bot_g_rightarm",
        Name = "Right Actuator Strike",
        Description = "Dominant arm actuator fires with full hydraulic power.",
        BodyPart = BodyPart.RightArm,
        SatisfiesTags = new() { "arm", "right", "upper" },
        BasePower = 3,
        BaseDefense = 1,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Approach,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftLeg = new()
    {
        Id = "bot_g_leftleg",
        Name = "Left Locomotor",
        Description = "Left leg servos drive an approach or repositioning step.",
        BodyPart = BodyPart.LeftLeg,
        SatisfiesTags = new() { "leg", "left", "lower" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 2,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Approach,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightLeg = new()
    {
        Id = "bot_g_rightleg",
        Name = "Right Locomotor",
        Description = "Right leg servos push forward for a drove-step.",
        BodyPart = BodyPart.RightLeg,
        SatisfiesTags = new() { "leg", "right", "lower" },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Approach,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Core = new()
    {
        Id = "bot_g_core",
        Name = "Reactor Core Surge",
        Description = "Diverting reactor output to limbs for a burst of raw force.",
        BodyPart = BodyPart.Core,
        SatisfiesTags = new() { "core", "body", "upper", "lower" },
        BasePower = 3,
        BaseDefense = 2,
        BaseSpeed = -2,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Stance = new()
    {
        Id = "bot_g_stance",
        Name = "Servo-Step",
        Description = "Precision servo control optimises footwork placement.",
        BodyPart = BodyPart.Stance,
        SatisfiesTags = new() { "stance", "lower", "movement" },
        BasePower = 0,
        BaseDefense = 2,
        BaseSpeed = 3,
        MinMovement = 1,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 1,
        BaseCooldown = 1,
    };

    // ===== UNIQUE CARDS =====
    // All base cooldowns ≥ 5. Energy costs and mode synergies applied by AdaptiveRobotPersona.

    /// <summary>ARM — costs 2 energy. Assault Mode: +2 extra Power.</summary>
    public static readonly UniqueCard U_PowerCoreStrike = new()
    {
        Id = "bot_u_core_strike",
        Name = "Power Core Strike",
        Description = "Channels reactor power through the arm for a devastating blow. Costs 2 energy.",
        RequiredBodyTags = new() { "arm" },
        BasePower = 4,
        BaseDefense = 0,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Approach,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.Head,
        BaseCooldown = 5,
    };

    /// <summary>LOWER/CORE — costs 2 energy. Assault Mode: +2 extra Power, gains Knockback.</summary>
    public static readonly UniqueCard U_HydraulicSlam = new()
    {
        Id = "bot_u_hydraulic_slam",
        Name = "Hydraulic Slam",
        Description = "Full-body hydraulic surge slams the opponent backward. Costs 2 energy.",
        RequiredBodyTags = new() { "lower", "core" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Knockback) },
        BasePower = 4,
        BaseDefense = -1,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Approach,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.Core,
        BaseCooldown = 5,
    };

    /// <summary>UPPER — costs 1 energy. Assault Mode: +1 Power, +1 Speed.</summary>
    public static readonly UniqueCard U_TargetingLock = new()
    {
        Id = "bot_u_targeting_lock",
        Name = "Targeting Lock",
        Description = "Sensor array locks a precise strike trajectory. Piercing. Costs 1 energy.",
        RequiredBodyTags = new() { "upper" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Piercing) },
        BasePower = 3,
        BaseDefense = 1,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = Models.Fighter.BodyLocation.Head,
        SecondaryTarget = Models.Fighter.BodyLocation.Torso,
        BaseCooldown = 5,
    };

    /// <summary>UPPER — costs 1 energy. Shield Mode: +2 extra Defense.</summary>
    public static readonly UniqueCard U_EnergyBarrier = new()
    {
        Id = "bot_u_energy_barrier",
        Name = "Energy Barrier",
        Description = "Projects a reactive energy field to absorb strikes. Guard. Costs 1 energy.",
        RequiredBodyTags = new() { "upper" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Guard) },
        BasePower = 0,
        BaseDefense = 4,
        BaseSpeed = 2,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Retreat,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.Head,
        BaseCooldown = 5,
    };

    /// <summary>LEG/STANCE — costs 1 energy. Speed Mode: +2 extra Speed.</summary>
    public static readonly UniqueCard U_OverdriveDash = new()
    {
        Id = "bot_u_overdrive_dash",
        Name = "Overdrive Dash",
        Description = "Servo overdrive launches an accelerated charge. Lunge. Costs 1 energy.",
        RequiredBodyTags = new() { "leg", "stance" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Lunge) },
        BasePower = 2,
        BaseDefense = 0,
        BaseSpeed = 3,
        MinMovement = 1,
        MaxMovement = 3,
        BaseMovementType = MovementType.Approach,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = Models.Fighter.BodyLocation.Stance,
        SecondaryTarget = Models.Fighter.BodyLocation.LeftLeg,
        BaseCooldown = 5,
    };

    /// <summary>CORE/BODY — costs 2 energy. Repair Mode: restores 1 extra damage step on a damaged location.</summary>
    public static readonly UniqueCard U_SelfRepairProtocol = new()
    {
        Id = "bot_u_self_repair",
        Name = "Self-Repair Protocol",
        Description = "Nanite swarms patch damaged systems. Costs 2 energy; Repair Mode adds extra restoration.",
        RequiredBodyTags = new() { "core", "body" },
        BasePower = 1,
        BaseDefense = 3,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Retreat,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Core,
        SecondaryTarget = Models.Fighter.BodyLocation.RightArm,
        BaseCooldown = 6,
    };

    /// <summary>ARM — costs 2 energy. Bleed + ArmorBreak.</summary>
    public static readonly UniqueCard U_ArcDischarge = new()
    {
        Id = "bot_u_arc_discharge",
        Name = "Arc Discharge",
        Description = "Releases stored static charge in a corrosive electrical burst. Bleed, ArmorBreak. Costs 2 energy.",
        RequiredBodyTags = new() { "arm" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Bleed), new CardKeywordValue(CardKeyword.ArmorBreak) },
        BasePower = 3,
        BaseDefense = 0,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.LeftArm,
        BaseCooldown = 6,
    };

    /// <summary>ANY — costs 1 energy. +1 Power per energy above 2 before spending.</summary>
    public static readonly UniqueCard U_KineticAmplifier = new()
    {
        Id = "bot_u_kinetic_amp",
        Name = "Kinetic Amplifier",
        Description = "Converts stored kinetic energy to strike force. Costs 1 energy; +1 Power per energy above 2.",
        RequiredBodyTags = new(),
        BasePower = 3,
        BaseDefense = -1,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.RightArm,
        BaseCooldown = 5,
    };

    /// <summary>HEAD/UPPER — costs 0 energy. Parry. Shield Mode: +2 extra Defense.</summary>
    public static readonly UniqueCard U_EmergencyVent = new()
    {
        Id = "bot_u_emergency_vent",
        Name = "Emergency Vent",
        Description = "Vents pressurised coolant to intercept and redirect an incoming strike. Parry. Free to use.",
        RequiredBodyTags = new() { "head", "upper" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Parry) },
        BasePower = 0,
        BaseDefense = 5,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Retreat,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Head,
        SecondaryTarget = Models.Fighter.BodyLocation.Torso,
        BaseCooldown = 5,
    };

    /// <summary>ARM — costs ALL energy, +2 Power per energy consumed. Crushing.</summary>
    public static readonly UniqueCard U_OverloadStrike = new()
    {
        Id = "bot_u_overload_strike",
        Name = "Overload Strike",
        Description = "Dumps the entire energy reserve into one devastating blow. Costs all energy; +2 Power per token spent. Crushing.",
        RequiredBodyTags = new() { "arm" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Crushing) },
        BasePower = 2,
        BaseDefense = -2,
        BaseSpeed = -2,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Retreat,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Head,
        SecondaryTarget = Models.Fighter.BodyLocation.Torso,
        BaseCooldown = 7,
    };

    /// <summary>CORE/BODY — costs 1 energy. Pull.</summary>
    public static readonly UniqueCard U_MagneticPull = new()
    {
        Id = "bot_u_magnetic_pull",
        Name = "Magnetic Pull",
        Description = "Electromagnetic vortex forces the opponent closer. Pull. Costs 1 energy.",
        RequiredBodyTags = new() { "core", "body" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Pull) },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 3,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.Core,
        BaseCooldown = 5,
    };

    /// <summary>STANCE/LOWER — costs 1 energy. Sidestep. Speed Mode: +1 extra Speed.</summary>
    public static readonly UniqueCard U_CombatProtocolShift = new()
    {
        Id = "bot_u_combat_protocol",
        Name = "Combat Protocol Shift",
        Description = "Evasive subroutine sidesteps and repositions for counter-position. Sidestep. Costs 1 energy.",
        RequiredBodyTags = new() { "stance", "lower" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Sidestep) },
        BasePower = 1,
        BaseDefense = 3,
        BaseSpeed = 2,
        MinMovement = 1,
        MaxMovement = 2,
        BaseMovementType = MovementType.Retreat,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = Models.Fighter.BodyLocation.Stance,
        SecondaryTarget = Models.Fighter.BodyLocation.LeftLeg,
        BaseCooldown = 5,
    };

    // ===== FIGHTER DEFINITION =====

    public static FighterDefinition CreateDefinition() => new()
    {
        Id = FighterId,
        Name = "Adaptive Robot",
        Description = "A mechanical combat unit with an energy token bank and five operational modes. Unique cards spend energy on use; depleted energy cripples offense, while overflow damages the robot itself.",
        GenericCards = new()
        {
            G_Head.Clone(), G_Torso.Clone(), G_LeftArm.Clone(), G_RightArm.Clone(),
            G_LeftLeg.Clone(), G_RightLeg.Clone(), G_Core.Clone(), G_Stance.Clone(),
        },
        UniqueCards = new()
        {
            U_PowerCoreStrike.Clone(),
            U_HydraulicSlam.Clone(),
            U_TargetingLock.Clone(),
            U_EnergyBarrier.Clone(),
            U_OverdriveDash.Clone(),
            U_SelfRepairProtocol.Clone(),
            U_ArcDischarge.Clone(),
            U_KineticAmplifier.Clone(),
            U_EmergencyVent.Clone(),
            U_OverloadStrike.Clone(),
            U_MagneticPull.Clone(),
            U_CombatProtocolShift.Clone(),
        },
        SpecialCards = new(),
        CriticalLocations = new() { Models.Fighter.BodyLocation.Head, Models.Fighter.BodyLocation.Torso },
        KOThreshold = 2,
        RankedUnlockWins = 15,
        Persona = AdaptiveRobotPersona.Instance,
    };
}
