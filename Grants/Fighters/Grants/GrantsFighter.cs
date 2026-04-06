using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Upgrades;

namespace Grants.Fighters.Grants;

/// <summary>
/// "Grants" — the starter fighter. A balanced brawler who rewards aggression.
/// Archetype: close-range pressure, moderate speed, high power on arm attacks.
/// 8 generics, 8 uniques, 2 specials.
/// All Name/Description strings marked _pl for content writing pass.
/// </summary>
public static class GrantsFighter
{
    public const string FighterId = "grants";

    // ===== GENERIC CARDS =====
    // Base cooldown: 1 turn each. Speed range: -1 to +1.

    public static readonly GenericCard G_Head = new()
    {
        Id = "grants_g_head",
        Name = "Head Strike_pl",
        Description = "A headbutt or shoulder check. Short range, destabilizing._pl",
        BodyPart = BodyPart.Head,
        SatisfiesTags = new() { "head", "upper" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 0,
        BaseMovement = 0,
        BaseRange = RangeBracket.Adjacent,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Torso = new()
    {
        Id = "grants_g_torso",
        Name = "Body Turn_pl",
        Description = "Rotating the torso to generate or absorb force._pl",
        BodyPart = BodyPart.Torso,
        SatisfiesTags = new() { "torso", "upper", "body" },
        BasePower = 2,
        BaseDefense = 2,
        BaseSpeed = -1,
        BaseMovement = 0,
        BaseRange = RangeBracket.Adjacent,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftArm = new()
    {
        Id = "grants_g_leftarm",
        Name = "Left Jab_pl",
        Description = "A quick probing strike with the left arm._pl",
        BodyPart = BodyPart.LeftArm,
        SatisfiesTags = new() { "arm", "left", "upper" },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 1,
        BaseMovement = 0,
        BaseRange = RangeBracket.Adjacent,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightArm = new()
    {
        Id = "grants_g_rightarm",
        Name = "Right Cross_pl",
        Description = "A powerful driving punch with the dominant arm._pl",
        BodyPart = BodyPart.RightArm,
        SatisfiesTags = new() { "arm", "right", "upper" },
        BasePower = 3,
        BaseDefense = 1,
        BaseSpeed = 0,
        BaseMovement = 0,
        BaseRange = RangeBracket.Adjacent,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftLeg = new()
    {
        Id = "grants_g_leftleg",
        Name = "Left Step_pl",
        Description = "Advancing or repositioning with the left leg._pl",
        BodyPart = BodyPart.LeftLeg,
        SatisfiesTags = new() { "leg", "left", "lower" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 1,
        BaseMovement = 1,
        BaseRange = RangeBracket.Adjacent,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightLeg = new()
    {
        Id = "grants_g_rightleg",
        Name = "Right Kick_pl",
        Description = "A solid kick from the power leg._pl",
        BodyPart = BodyPart.RightLeg,
        SatisfiesTags = new() { "leg", "right", "lower" },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 0,
        BaseMovement = 1,
        BaseRange = RangeBracket.Adjacent,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Core = new()
    {
        Id = "grants_g_core",
        Name = "Center Drive_pl",
        Description = "Driving from the hips — powers grapples and slams._pl",
        BodyPart = BodyPart.Core,
        SatisfiesTags = new() { "core", "body", "upper", "lower" },
        BasePower = 2,
        BaseDefense = 2,
        BaseSpeed = -1,
        BaseMovement = 0,
        BaseRange = RangeBracket.Adjacent,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Stance = new()
    {
        Id = "grants_g_stance",
        Name = "Footwork_pl",
        Description = "Adjusting position and posture to create or deny openings._pl",
        BodyPart = BodyPart.Stance,
        SatisfiesTags = new() { "stance", "lower", "movement" },
        BasePower = 0,
        BaseDefense = 2,
        BaseSpeed = 2,
        BaseMovement = 2,
        BaseRange = RangeBracket.Close,
        BaseCooldown = 1,
    };

    // ===== UNIQUE CARDS =====
    // Base cooldown: 2 turns.

    public static readonly UniqueCard U_Haymaker = new()
    {
        Id = "grants_u_haymaker",
        Name = "Haymaker_pl",
        Description = "A wide overhand swing trading speed for devastating power._pl",
        RequiredBodyTags = new() { "arm" },
        BasePower = 4,
        BaseDefense = 0,
        BaseSpeed = -2,
        BaseMovement = 0,
        BaseRange = RangeBracket.Adjacent,
        BaseCooldown = 2,
    };

    public static readonly UniqueCard U_Clinch = new()
    {
        Id = "grants_u_clinch",
        Name = "Clinch_pl",
        Description = "Grabbing the opponent to control range and restrict their options._pl",
        RequiredBodyTags = new() { "arm", "upper" },
        Keywords = new() { CardKeyword.Stagger },
        BasePower = 2,
        BaseDefense = 2,
        BaseSpeed = 0,
        BaseMovement = 0,
        BaseRange = RangeBracket.Adjacent,
        BaseCooldown = 2,
    };

    public static readonly UniqueCard U_CrossCounter = new()
    {
        Id = "grants_u_crosscounter",
        Name = "Cross Counter_pl",
        Description = "Slipping an incoming blow and returning with a sharp counter._pl",
        RequiredBodyTags = new() { "arm" },
        Keywords = new() { CardKeyword.Parry },
        BasePower = 3,
        BaseDefense = 3,
        BaseSpeed = 1,
        BaseMovement = 0,
        BaseRange = RangeBracket.Adjacent,
        BaseCooldown = 2,
    };

    public static readonly UniqueCard U_BullRush = new()
    {
        Id = "grants_u_bullrush",
        Name = "Bull Rush_pl",
        Description = "Charging forward to close distance and drive the opponent back._pl",
        RequiredBodyTags = new() { "lower", "core" },
        Keywords = new() { CardKeyword.Lunge, CardKeyword.Knockback },
        BasePower = 3,
        BaseDefense = 0,
        BaseSpeed = 0,
        BaseMovement = 2,
        BaseRange = RangeBracket.Close,
        BaseCooldown = 2,
    };

    public static readonly UniqueCard U_LowSweep = new()
    {
        Id = "grants_u_lowsweep",
        Name = "Low Sweep_pl",
        Description = "A fast sweeping kick targeting the legs to disrupt balance._pl",
        RequiredBodyTags = new() { "leg" },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 2,
        BaseMovement = 0,
        BaseRange = RangeBracket.Adjacent,
        BaseCooldown = 2,
    };

    public static readonly UniqueCard U_Overhand = new()
    {
        Id = "grants_u_overhand",
        Name = "Overhand_pl",
        Description = "A looping blow aimed at the top of the opponent's guard._pl",
        RequiredBodyTags = new() { "arm", "upper" },
        Keywords = new() { CardKeyword.ArmorBreak },
        BasePower = 3,
        BaseDefense = 0,
        BaseSpeed = -1,
        BaseMovement = 0,
        BaseRange = RangeBracket.Adjacent,
        BaseCooldown = 2,
    };

    public static readonly UniqueCard U_SideStep = new()
    {
        Id = "grants_u_sidestep",
        Name = "Sidestep_pl",
        Description = "Slipping laterally to avoid an attack and reset position._pl",
        RequiredBodyTags = new() { "stance", "lower" },
        Keywords = new() { CardKeyword.Sidestep },
        BasePower = 0,
        BaseDefense = 3,
        BaseSpeed = 3,
        BaseMovement = 2,
        BaseRange = RangeBracket.Close,
        BaseCooldown = 2,
    };

    public static readonly UniqueCard U_BodyShot = new()
    {
        Id = "grants_u_bodyshot",
        Name = "Body Shot_pl",
        Description = "Targeting the torso to wear down stamina and power._pl",
        RequiredBodyTags = new() { "arm", "core" },
        Keywords = new() { CardKeyword.Bleed },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 0,
        BaseMovement = 0,
        BaseRange = RangeBracket.Adjacent,
        BaseCooldown = 2,
    };

    // ===== SPECIAL CARDS =====
    // Base cooldown: 3 turns.

    public static readonly SpecialCard S_Obliterator = new()
    {
        Id = "grants_s_obliterator",
        Name = "Obliterator_pl",
        Description = "A cataclysmic full-body strike that channels everything into a single devastating blow._pl",
        Keywords = new() { CardKeyword.Crushing, CardKeyword.ArmorBreak },
        BasePower = 7,
        BaseDefense = 0,
        BaseSpeed = -2,
        BaseMovement = 0,
        BaseRange = RangeBracket.Adjacent,
        BaseCooldown = 3,
        RequiredRange = RangeBracket.Adjacent,
    };

    public static readonly SpecialCard S_BerserkRush = new()
    {
        Id = "grants_s_berserkrish",
        Name = "Berserk Rush_pl",
        Description = "A relentless burst of strikes abandoning all defense._pl",
        Keywords = new() { CardKeyword.Lunge, CardKeyword.Bleed },
        BasePower = 5,
        BaseDefense = -1,
        BaseSpeed = 1,
        BaseMovement = 3,
        BaseRange = RangeBracket.Close,
        BaseCooldown = 3,
        Standalone = true,
    };

    // ===== FIGHTER DEFINITION =====
    public static FighterDefinition CreateDefinition() => new()
    {
        Id = FighterId,
        Name = "Grants_pl",
        Description = "A hard-hitting brawler who thrives in close quarters. Rewards aggression and punishes hesitation._pl",
        GenericCards = new() { G_Head, G_Torso, G_LeftArm, G_RightArm, G_LeftLeg, G_RightLeg, G_Core, G_Stance },
        UniqueCards = new() { U_Haymaker, U_Clinch, U_CrossCounter, U_BullRush, U_LowSweep, U_Overhand, U_SideStep, U_BodyShot },
        SpecialCards = new() { S_Obliterator, S_BerserkRush },
        CriticalLocations = new() { Models.Fighter.BodyLocation.Head, Models.Fighter.BodyLocation.Torso },
        KOThreshold = 2,
        RankedUnlockWins = 15,
    };
}
