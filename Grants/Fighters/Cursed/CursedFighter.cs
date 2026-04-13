using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Upgrades;

namespace Grants.Fighters.Cursed;

/// <summary>
/// "The Cursed" — a volatile token-based fighter who curses opponents and feeds off the chaos.
///
/// Archetype: mid-range pressure, builds curse tokens on every hit and transfers them to
/// opponents who must decide each round whether to burn a token for a weaker (but potentially
/// refunded) hit, or let the curse stack grow.
///
/// 8 generics  — serviceable stats, all body parts covered.
/// 6 uniques   — curse keyword synergies (CurseGain, CursePull, CurseEmpower, CurseWeaken).
/// 2 specials  — high-value curse combos, both standalone capable.
/// </summary>
public static class CursedFighter
{
    public const string FighterId = "cursed";

    // ===== GENERIC CARDS =====

    public static readonly GenericCard G_Head = new()
    {
        Id = "cursed_g_head",
        Name = "Haunted Headbutt",
        Description = "A desperate headbutt. Short range, disrupting.",
        BodyPart = BodyPart.Head,
        SatisfiesTags = new() { "head", "upper" },
        BasePower = 1,
        BaseDefense = 1,
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
        Id = "cursed_g_torso",
        Name = "Withered Frame",
        Description = "Turning the torso to absorb blows or generate momentum.",
        BodyPart = BodyPart.Torso,
        SatisfiesTags = new() { "torso", "upper", "body" },
        BasePower = 2,
        BaseDefense = 2,
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
        Id = "cursed_g_leftarm",
        Name = "Left Claw",
        Description = "A reaching swipe with the left arm.",
        BodyPart = BodyPart.LeftArm,
        SatisfiesTags = new() { "arm", "left", "upper" },
        BasePower = 2,
        BaseDefense = 0,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightArm = new()
    {
        Id = "cursed_g_rightarm",
        Name = "Right Strike",
        Description = "A firm blow with the dominant arm.",
        BodyPart = BodyPart.RightArm,
        SatisfiesTags = new() { "arm", "right", "upper" },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Retreat,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftLeg = new()
    {
        Id = "cursed_g_leftleg",
        Name = "Dragging Step",
        Description = "A lurching advance on the left leg.",
        BodyPart = BodyPart.LeftLeg,
        SatisfiesTags = new() { "leg", "left", "lower" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Approach,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightLeg = new()
    {
        Id = "cursed_g_rightleg",
        Name = "Cursed Kick",
        Description = "A stomping kick that shakes the ground.",
        BodyPart = BodyPart.RightLeg,
        SatisfiesTags = new() { "leg", "right", "lower" },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Approach,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Core = new()
    {
        Id = "cursed_g_core",
        Name = "Dark Center",
        Description = "Channeling dark energy from the core.",
        BodyPart = BodyPart.Core,
        SatisfiesTags = new() { "core", "body", "upper", "lower" },
        BasePower = 2,
        BaseDefense = 2,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Stance = new()
    {
        Id = "cursed_g_stance",
        Name = "Shambling Footwork",
        Description = "Erratic repositioning that is hard to predict.",
        BodyPart = BodyPart.Stance,
        SatisfiesTags = new() { "stance", "lower", "movement" },
        BasePower = 0,
        BaseDefense = 2,
        BaseSpeed = 2,
        MinMovement = 1,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 1,
        BaseCooldown = 1,
    };

    // ===== UNIQUE CARDS =====

    /// <summary>
    /// Wretched Strike — CurseGain: on hit, gain 1 EXTRA token to pool (on top of base gain).
    /// Quick arm attack that rewards aggressive pool-building.
    /// </summary>
    public static readonly UniqueCard U_WretchedStrike = new()
    {
        Id = "cursed_u_wretchedstrike",
        Name = "Wretched Strike",
        Description = "A desperate blow that bleeds dark energy into The Cursed's pool.",
        RequiredBodyTags = new() { "arm", "upper" },
        Keywords = new() { new CardKeywordValue(CardKeyword.CurseGain) },
        BasePower = 2,
        BaseDefense = 0,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Head,
        SecondaryTarget = Models.Fighter.BodyLocation.Torso,
        BaseCooldown = 2,
    };

    /// <summary>
    /// Phantom Pull — CursePull: on hit, pull opponent N hexes toward self (N = their curse tokens).
    /// Ranged grab that rewards stacking curse tokens on the opponent.
    /// </summary>
    public static readonly UniqueCard U_PhantomPull = new()
    {
        Id = "cursed_u_phantompull",
        Name = "Phantom Pull",
        Description = "Dark tendrils yank the opponent closer, one hex per Curse token they carry.",
        RequiredBodyTags = new() { },
        Keywords = new() { new CardKeywordValue(CardKeyword.CursePull) },
        BasePower = 2,
        BaseDefense = 0,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Retreat,
        MinRange = 2,
        MaxRange = 3,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.Core,
        BaseCooldown = 2,
    };

    /// <summary>
    /// Dark Empowerment — CurseEmpower: +N power this attack (N = owner's pool count).
    /// A slow, pool-draining power swing. Devastating at max pool.
    /// </summary>
    public static readonly UniqueCard U_DarkEmpowerment = new()
    {
        Id = "cursed_u_darkempowerment",
        Name = "Dark Empowerment",
        Description = "Feeds on the cursed pool for extra power. Empty the pool, unleash devastation.",
        RequiredBodyTags = new() { "arm", "upper" },
        Keywords = new() { new CardKeywordValue(CardKeyword.CurseEmpower) },
        BasePower = 0,
        BaseDefense = 0,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Retreat,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.LeftArm,
        BaseCooldown = 2,
    };

    /// <summary>
    /// Curse Lash — CurseWeaken: reduce opponent defense by N (N = their curse tokens).
    /// Mid-range attack that scales in power the more tokens the opponent is holding.
    /// </summary>
    public static readonly UniqueCard U_CurseLash = new()
    {
        Id = "cursed_u_curselash",
        Name = "Curse Lash",
        Description = "Whips out against the opponent's defenses, weakened by their own curse tokens.",
        RequiredBodyTags = new() { "arm" },
        Keywords = new() { new CardKeywordValue(CardKeyword.CurseWeaken) },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = Models.Fighter.BodyLocation.LeftArm,
        SecondaryTarget = Models.Fighter.BodyLocation.Head,
        BaseCooldown = 2,
    };

    /// <summary>
    /// Hex Barrage — CursePull + CurseEmpower: a combo attack that closes range AND empowers itself.
    /// Slower but applies both effects at once.
    /// </summary>
    public static readonly UniqueCard U_HexBarrage = new()
    {
        Id = "cursed_u_hexbarrage",
        Name = "Hex Barrage",
        Description = "A barrage of dark blows that pulls the enemy in while empowered by the pool.",
        RequiredBodyTags = new() { "arm", "upper" },
        Keywords = new() { new CardKeywordValue(CardKeyword.CursePull), new CardKeywordValue(CardKeyword.CurseEmpower) },
        BasePower = 1,
        BaseDefense = 0,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.RightArm,
        BaseCooldown = 2,
    };

    /// <summary>
    /// Mark of Doom — Stagger: a decisive strike that stuns and positions for follow-up.
    /// </summary>
    public static readonly UniqueCard U_MarkOfDoom = new()
    {
        Id = "cursed_u_markofdoom",
        Name = "Mark of Doom",
        Description = "A strike that staggers the opponent, marking them for devastation.",
        RequiredBodyTags = new() { "upper", "head" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Stagger) },
        BasePower = 3,
        BaseDefense = 0,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Approach,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Head,
        SecondaryTarget = Models.Fighter.BodyLocation.Stance,
        BaseCooldown = 2,
    };

    // ===== SPECIAL CARDS =====

    /// <summary>
    /// Cursed Binding — Stagger + CurseGain. Standalone. Short range grapple that stuns
    /// and drains the pool-building mechanic for an extra token.
    /// </summary>
    public static readonly SpecialCard S_CursedBinding = new()
    {
        Id = "cursed_s_cursed_binding",
        Name = "Cursed Binding",
        Description = "Dark chains erupt from The Cursed, staggering the opponent and filling the pool.",
        Keywords = new() { new CardKeywordValue(CardKeyword.Stagger), new CardKeywordValue(CardKeyword.CurseGain) },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.RightArm,
        SecondaryTarget = Models.Fighter.BodyLocation.Torso,
        BaseCooldown = 2,
        Standalone = true,
    };

    /// <summary>
    /// Curse Unleashed — CurseEmpower + CurseWeaken. Standalone. A devastating combo that
    /// scales off both sides: power from the pool, defense reduction from opponent's tokens.
    /// </summary>
    public static readonly SpecialCard S_CurseUnleashed = new()
    {
        Id = "cursed_s_curse_unleashed",
        Name = "Curse Unleashed",
        Description = "The Cursed pours out their pool in a savage strike, weakening the cursed opponent.",
        Keywords = new() { new CardKeywordValue(CardKeyword.CurseEmpower), new CardKeywordValue(CardKeyword.CurseWeaken) },
        BasePower = 1,
        BaseDefense = 0,
        BaseSpeed = -2,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Retreat,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.Core,
        BaseCooldown = 3,
        Standalone = true,
    };

    // ===== FIGHTER DEFINITION =====

    public static FighterDefinition CreateDefinition() => new()
    {
        Id = FighterId,
        Name = "The Cursed",
        Description = "A volatile token-based fighter. Curses the opponent every time they land a hit, " +
                      "then feeds the curse back into overwhelming, escalating damage.",
        Persona = CursedPersona.Instance,
        GenericCards = new()
        {
            G_Head.Clone(), G_Torso.Clone(), G_LeftArm.Clone(), G_RightArm.Clone(),
            G_LeftLeg.Clone(), G_RightLeg.Clone(), G_Core.Clone(), G_Stance.Clone(),
        },
        UniqueCards = new()
        {
            U_WretchedStrike.Clone(), U_PhantomPull.Clone(), U_DarkEmpowerment.Clone(),
            U_CurseLash.Clone(), U_HexBarrage.Clone(), U_MarkOfDoom.Clone(),
        },
        SpecialCards = new()
        {
            S_CursedBinding.Clone(), S_CurseUnleashed.Clone(),
        },
        CriticalLocations = new() { Models.Fighter.BodyLocation.Head, Models.Fighter.BodyLocation.Torso },
        KOThreshold = 2,
        RankedUnlockWins = 20,
    };
}
