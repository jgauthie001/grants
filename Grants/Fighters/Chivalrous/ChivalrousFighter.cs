using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Upgrades;

namespace Grants.Fighters.Chivalrous;

/// <summary>
/// "The Chivalrous" — a principled knight who fights with honour and rewards the opponent
/// for engaging directly.
///
/// Archetype: heavy-hitting mid-range fighter with strong defence at range.
/// His unique cards deal above-average damage, and several synergise with
/// the chivalry tokens he leaves on opponents:
///   - Righteous Blow: bonus damage step if opponent has tokens
///   - Binding Reach: long-range attack that pulls the opponent closer (Pull keyword)
///   - Iron Fortitude: MaxDamageCap protects a key location from being blown out in one hit
///   - Distant Bulwark: passive +2 defence bonus when fighting at range (DistanceGuard keyword)
///   - Honour Smite: combo finisher — extra damaging if opponent is carrying tokens
///
/// 6 generics (all body parts) | 5 uniques | 0 specials
/// </summary>
public static class ChivalrousFighter
{
    public const string FighterId = "chivalrous";

    // ===== GENERIC CARDS =====

    public static readonly GenericCard G_Head = new()
    {
        Id = "chiv_g_head",
        Name = "Helmet Strike",
        Description = "A headbutt reinforced by an iron will. Short range, hard to ignore.",
        BodyPart = BodyPart.Head,
        SatisfiesTags = new() { "head", "upper" },
        BasePower = 2,
        BaseDefense = 2,
        BaseSpeed = 0,
        MaxMovement = 0,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Torso = new()
    {
        Id = "chiv_g_torso",
        Name = "Shield Shoulder",
        Description = "Rotating behind the shield to absorb hits or drive a charge.",
        BodyPart = BodyPart.Torso,
        SatisfiesTags = new() { "torso", "upper", "body" },
        BasePower = 2,
        BaseDefense = 3,
        BaseSpeed = -1,
        MaxMovement = 0,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftArm = new()
    {
        Id = "chiv_g_leftarm",
        Name = "Shield Thrust",
        Description = "A direct push with the shield arm, creating space or an opening.",
        BodyPart = BodyPart.LeftArm,
        SatisfiesTags = new() { "arm", "left", "upper" },
        BasePower = 2,
        BaseDefense = 2,
        BaseSpeed = 0,
        MaxMovement = 0,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightArm = new()
    {
        Id = "chiv_g_rightarm",
        Name = "Sword Arm",
        Description = "Cocking the sword arm back for a strong strike.",
        BodyPart = BodyPart.RightArm,
        SatisfiesTags = new() { "arm", "right", "upper" },
        BasePower = 4,
        BaseDefense = 1,
        BaseSpeed = 0,
        MaxMovement = 0,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftLeg = new()
    {
        Id = "chiv_g_leftleg",
        Name = "Advance Step",
        Description = "Stepping forward to close distance deliberately.",
        BodyPart = BodyPart.LeftLeg,
        SatisfiesTags = new() { "leg", "left", "lower" },
        BasePower = 1,
        BaseDefense = 2,
        BaseSpeed = 1,
        MinMovement = 1,
        MaxMovement = 2,
        BaseMovementType = MovementType.Approach,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightLeg = new()
    {
        Id = "chiv_g_rightleg",
        Name = "Charging Leg",
        Description = "A powerful driving leg that builds into a charge strike.",
        BodyPart = BodyPart.RightLeg,
        SatisfiesTags = new() { "leg", "right", "lower" },
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

    // ===== UNIQUE CARDS =====

    /// <summary>
    /// Righteous Blow — strong hit, bonus damage if opponent carries chivalry tokens.
    /// Pairs with any arm generic.
    /// </summary>
    public static readonly UniqueCard U_RighteousBlow = new()
    {
        Id = "chiv_u_righteousblow",
        Name = "Righteous Blow",
        Description = "A heavy overhead strike. Hits harder against those who carry honour.",
        RequiredBodyTags = new() { "arm" },
        Keywords = new() { new CardKeywordValue(CardKeyword.ChivalryBonus, 1) },
        BasePower = 4,
        BaseDefense = 0,
        BaseSpeed = -1,
        MaxMovement = 0,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Head,
        SecondaryTarget = Models.Fighter.BodyLocation.Torso,
        BaseCooldown = 2,
    };

    /// <summary>
    /// Iron Fortitude — defensive card with MaxDamageCap to prevent a location being blown out.
    /// Pairs with Shield Shoulder (torso) or Shield Thrust (left arm).
    /// </summary>
    public static readonly UniqueCard U_IronFortitude = new()
    {
        Id = "chiv_u_ironfortitude",
        Name = "Iron Fortitude",
        Description = "A steadfast guard that ensures no single hit can cripple a vital location.",
        RequiredBodyTags = new() { "upper" },
        Keywords = new()
        {
            new CardKeywordValue(CardKeyword.Guard),
            new CardKeywordValue(CardKeyword.MaxDamageCap, 2),  // cap at Injured
        },
        BasePower = 1,
        BaseDefense = 4,
        BaseSpeed = 0,
        MaxMovement = 0,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.Torso,
        BaseCooldown = 2,
    };

    /// <summary>
    /// Binding Reach — long-range attack that pulls the opponent 1 hex closer on hit.
    /// Pairs with any arm generic. Punishes opponents trying to maintain distance.
    /// </summary>
    public static readonly UniqueCard U_BindingReach = new()
    {
        Id = "chiv_u_bindingreachg",
        Name = "Binding Reach",
        Description = "A long sweeping strike that hooks the opponent and drags them into range.",
        RequiredBodyTags = new() { "arm", "right" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Pull) },
        BasePower = 3,
        BaseDefense = 0,
        BaseSpeed = 0,
        MaxMovement = 0,
        MinRange = 2,
        MaxRange = 3,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.LeftArm,
        BaseCooldown = 2,
    };

    /// <summary>
    /// Distant Bulwark — a defensive stance that gains extra protection when the opponent is far away.
    /// DistanceGuard (value 3): +2 defence if current distance >= 3.
    /// Pairs with any generic (good on torso or arm).
    /// </summary>
    public static readonly UniqueCard U_DistantBulwark = new()
    {
        Id = "chiv_u_distantbulwark",
        Name = "Distant Bulwark",
        Description = "A ranged guard stance. The farther the attacker, the stronger the protection.",
        RequiredBodyTags = new() { "upper" },
        Keywords = new()
        {
            new CardKeywordValue(CardKeyword.DistanceGuard, 3),
            new CardKeywordValue(CardKeyword.MaxDamageCap, 2),
        },
        BasePower = 0,
        BaseDefense = 3,
        BaseSpeed = +1,
        MaxMovement = 0,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.Torso,
        BaseCooldown = 2,
    };

    /// <summary>
    /// Honour Smite — powerful finishing strike, bonus damage if opponent has chivalry tokens.
    /// Uses the charging leg as the base.
    /// </summary>
    public static readonly UniqueCard U_HonourSmite = new()
    {
        Id = "chiv_u_honoursmite",
        Name = "Honour Smite",
        Description = "A devastating lunge that lands hardest on those burdened by honour.",
        RequiredBodyTags = new() { "leg", "lower" },
        Keywords = new()
        {
            new CardKeywordValue(CardKeyword.ChivalryBonus, 1),
            new CardKeywordValue(CardKeyword.Crushing),
        },
        BasePower = 3,
        BaseDefense = 0,
        BaseSpeed = -1,
        MinMovement = 1,
        MaxMovement = 1,
        BaseMovementType = MovementType.Approach,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.Core,
        BaseCooldown = 2,
    };

    // ===== FIGHTER DEFINITION =====

    public static FighterDefinition CreateDefinition() => new FighterDefinition
    {
        Id          = FighterId,
        Name        = "The Chivalrous",
        Description = "A knight who honours opponents with tokens - then punishes them for it.",
        Persona     = ChivalrousPersona.Instance,
        RankedUnlockWins = 10,
        GenericCards = new()
        {
            G_Head, G_Torso, G_LeftArm, G_RightArm, G_LeftLeg, G_RightLeg,
        },
        UniqueCards = new()
        {
            U_RighteousBlow,
            U_IronFortitude,
            U_BindingReach,
            U_DistantBulwark,
            U_HonourSmite,
        },
        SpecialCards = new(),
    };
}
