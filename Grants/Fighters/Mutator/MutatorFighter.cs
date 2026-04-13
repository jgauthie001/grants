using Grants.Models.Cards;
using Grants.Models.Fighter;

namespace Grants.Fighters.Mutator;

/// <summary>
/// The Mutator — an adaptable fighter who recycles cooling cards as mutations.
/// Each unique card is individually weaker than a standard fighter's,
/// but the Mutator always plays three cards per round (Generic + Unique + Mutation),
/// making total output competitive while offering high strategic variance.
///
/// 8 generics (one per body part), 7 unique cards with short cooldowns (cd=2-3),
/// 0 specials. Unique cards cover all body-part tag groups so the mutation pool
/// stays broad throughout the match.
/// </summary>
public static class MutatorFighter
{
    public const string FighterId = "mutator";

    // ===== GENERIC CARDS =====

    public static readonly GenericCard G_Head = new()
    {
        Id = "mutator_g_head",
        Name = "Snap Head",
        Description = "A quick headbutt or skull redirect to destabilize.",
        BodyPart = BodyPart.Head,
        SatisfiesTags = new() { "head", "upper" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Retreat,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Torso = new()
    {
        Id = "mutator_g_torso",
        Name = "Body Lean",
        Description = "Rotating the torso to absorb or redirect force.",
        BodyPart = BodyPart.Torso,
        SatisfiesTags = new() { "torso", "upper", "body" },
        BasePower = 2,
        BaseDefense = 3,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Retreat,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftArm = new()
    {
        Id = "mutator_g_leftarm",
        Name = "Left Lash",
        Description = "A probing jab that primes the follow-through.",
        BodyPart = BodyPart.LeftArm,
        SatisfiesTags = new() { "arm", "left", "upper" },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 2,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightArm = new()
    {
        Id = "mutator_g_rightarm",
        Name = "Right Lash",
        Description = "A faster drive from the right side.",
        BodyPart = BodyPart.RightArm,
        SatisfiesTags = new() { "arm", "right", "upper" },
        BasePower = 3,
        BaseDefense = 1,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftLeg = new()
    {
        Id = "mutator_g_leftleg",
        Name = "Step Left",
        Description = "Advancing or slipping to the left.",
        BodyPart = BodyPart.LeftLeg,
        SatisfiesTags = new() { "leg", "left", "lower" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Approach,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightLeg = new()
    {
        Id = "mutator_g_rightleg",
        Name = "Step Right",
        Description = "Pressing forward from the right.",
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
        Id = "mutator_g_core",
        Name = "Core Brace",
        Description = "Centering weight to absorb and redirect.",
        BodyPart = BodyPart.Core,
        SatisfiesTags = new() { "core", "body", "upper", "lower" },
        BasePower = 1,
        BaseDefense = 4,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Stance = new()
    {
        Id = "mutator_g_stance",
        Name = "Drop Stance",
        Description = "Shifting base to threaten any angle.",
        BodyPart = BodyPart.Stance,
        SatisfiesTags = new() { "stance", "lower", "movement" },
        BasePower = 1,
        BaseDefense = 2,
        BaseSpeed = 2,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 1,
        BaseCooldown = 1,
    };

    // ===== UNIQUE CARDS =====
    // Individually weaker than standard fighters; the mutation bonus makes them competitive.
    // Short cooldowns (2) so there's usually a card available to mutate with.

    /// <summary>Arm strike — quick aggression. Pairs with arm generics.</summary>
    public static readonly UniqueCard U_FeralStrike = new()
    {
        Id = "mutator_u_feralstrike",
        Name = "Feral Strike",
        Description = "A short, instinctive blast of force.",
        RequiredBodyTags = new() { "arm" },
        Keywords = new(),
        BasePower = 3,
        BaseDefense = 0,
        BaseSpeed = 2,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = BodyLocation.LeftArm,
        SecondaryTarget = BodyLocation.Torso,
        BaseCooldown = 2,
    };

    /// <summary>Body/core defense. Pairs with torso or core generics.</summary>
    public static readonly UniqueCard U_ShiftingGuard = new()
    {
        Id = "mutator_u_shiftingguard",
        Name = "Shifting Guard",
        Description = "Morphing the guard to absorb incoming force.",
        RequiredBodyTags = new() { "body" },
        Keywords = new(),
        BasePower = 0,
        BaseDefense = 4,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Retreat,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = BodyLocation.Torso,
        SecondaryTarget = BodyLocation.Torso,
        BaseCooldown = 2,
    };

    /// <summary>Leg-driven repositioning strike. Pairs with leg or stance generics.</summary>
    public static readonly UniqueCard U_RapidDart = new()
    {
        Id = "mutator_u_rapiddart",
        Name = "Rapid Dart",
        Description = "A darting lunge that closes distance in a flash.",
        RequiredBodyTags = new() { "lower" },
        Keywords = new(),
        BasePower = 2,
        BaseDefense = 0,
        BaseSpeed = 3,
        MinMovement = 1,
        MaxMovement = 3,
        BaseMovementType = MovementType.Approach,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = BodyLocation.LeftLeg,
        SecondaryTarget = BodyLocation.Torso,
        BaseCooldown = 2,
    };

    /// <summary>Heavy upper-body swing. Pairs with any upper-body generic.</summary>
    public static readonly UniqueCard U_WildSwing = new()
    {
        Id = "mutator_u_wildswing",
        Name = "Wild Swing",
        Description = "A reckless arcing blow with bone-crushing weight.",
        RequiredBodyTags = new() { "upper" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Crushing) },
        BasePower = 4,
        BaseDefense = 0,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = BodyLocation.Torso,
        SecondaryTarget = BodyLocation.RightArm,
        BaseCooldown = 3,
    };

    /// <summary>Evasive repositioning with moderate pressure. Pairs with stance or lower generics.</summary>
    public static readonly UniqueCard U_Weave = new()
    {
        Id = "mutator_u_weave",
        Name = "Weave",
        Description = "Slipping sideways while keeping reach open.",
        RequiredBodyTags = new() { "stance", "lower" },
        Keywords = new(),
        BasePower = 1,
        BaseDefense = 2,
        BaseSpeed = 2,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 3,
        PrimaryTarget = BodyLocation.Torso,
        SecondaryTarget = BodyLocation.Torso,
        BaseCooldown = 2,
    };

    /// <summary>Pressuring head strike. Pairs with any upper-body generic.</summary>
    public static readonly UniqueCard U_AdaptivePress = new()
    {
        Id = "mutator_u_adaptivepress",
        Name = "Adaptive Press",
        Description = "A probing strike that reads and presses the opponent's high guard.",
        RequiredBodyTags = new() { "upper" },
        Keywords = new(),
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 2,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Approach,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = BodyLocation.Head,
        SecondaryTarget = BodyLocation.Torso,
        BaseCooldown = 2,
    };

    /// <summary>Flexible all-body defensive option. Pairs with any generic.</summary>
    public static readonly UniqueCard U_Contort = new()
    {
        Id = "mutator_u_contort",
        Name = "Contort",
        Description = "Twisting the whole body to absorb hits from any angle.",
        RequiredBodyTags = new(),   // pairs with any generic
        Keywords = new(),
        BasePower = 1,
        BaseDefense = 3,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = BodyLocation.Torso,
        SecondaryTarget = BodyLocation.Torso,
        BaseCooldown = 2,
    };

    // ===== FIGHTER DEFINITION =====

    public static FighterDefinition CreateDefinition() => new()
    {
        Id   = FighterId,
        Name = "Mutator",
        Description = "Adapts each round by recycling a unique card from cooldown as a third action. Individual cards are weaker, but the Mutator always has an answer.",
        Persona = MutatorPersona.Instance,
        GenericCards = new()
        {
            G_Head, G_Torso, G_LeftArm, G_RightArm,
            G_LeftLeg, G_RightLeg, G_Core, G_Stance,
        },
        UniqueCards = new()
        {
            U_FeralStrike, U_ShiftingGuard, U_RapidDart,
            U_WildSwing, U_Weave, U_AdaptivePress, U_Contort,
        },
        SpecialCards = new(),
        CriticalLocations = new() { BodyLocation.Head, BodyLocation.Torso },
        KOThreshold = 2,
    };
}
