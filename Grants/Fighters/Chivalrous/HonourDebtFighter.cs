using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Upgrades;

namespace Grants.Fighters.Chivalrous;

/// <summary>
/// "Honour Debt" — a ruthless variant of The Chivalrous.
/// Where The Chivalrous rewards the opponent's courage, Honour Debt exploits it.
///
/// Archetype: aggressive speed-pressure fighter. The more tokens on the opponent,
/// the faster Honour Debt becomes. When the opponent finally cashes out those tokens,
/// they get a big power spike — but Honour Debt may already be ahead in the exchange.
///
/// Key design differences from The Chivalrous:
///   - No ChivalryBonus cards (tokens don't affect hit damage)
///   - Emphasis on closing distance fast and staying in range
///   - DistanceGuard dropped in favour of offensive pressure cards
///   - Token spending: opponent gets 2 Power per token instead of 1 Power + 1 Speed
///
/// 6 generics (all body parts) | 5 uniques | 0 specials
/// </summary>
public static class HonourDebtFighter
{
    public const string FighterId = "honour_debt";

    // ===== GENERIC CARDS =====

    public static readonly GenericCard G_Head = new()
    {
        Id = "hd_g_head",
        Name = "Debt's Glare",
        Description = "A threatening headbutt that leaves no doubt about intent.",
        BodyPart = BodyPart.Head,
        SatisfiesTags = new() { "head", "upper" },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_Torso = new()
    {
        Id = "hd_g_torso",
        Name = "Debt's Weight",
        Description = "Leaning into the fight with the whole body, making space difficult.",
        BodyPart = BodyPart.Torso,
        SatisfiesTags = new() { "torso", "upper", "body" },
        BasePower = 3,
        BaseDefense = 2,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Approach,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftArm = new()
    {
        Id = "hd_g_leftarm",
        Name = "Grip Strike",
        Description = "Grabbing and striking in one motion, closing the gap decisively.",
        BodyPart = BodyPart.LeftArm,
        SatisfiesTags = new() { "arm", "left", "upper" },
        BasePower = 2,
        BaseDefense = 2,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Free,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightArm = new()
    {
        Id = "hd_g_rightarm",
        Name = "Collector's Strike",
        Description = "The main weapon arm, drives debts home.",
        BodyPart = BodyPart.RightArm,
        SatisfiesTags = new() { "arm", "right", "upper" },
        BasePower = 4,
        BaseDefense = 1,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Approach,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_LeftLeg = new()
    {
        Id = "hd_g_leftleg",
        Name = "Pursuit Step",
        Description = "Closing distance relentlessly - you cannot outrun what you owe.",
        BodyPart = BodyPart.LeftLeg,
        SatisfiesTags = new() { "leg", "left", "lower" },
        BasePower = 1,
        BaseDefense = 1,
        BaseSpeed = 2,
        MinMovement = 1,
        MaxMovement = 2,
        BaseMovementType = MovementType.Approach,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    public static readonly GenericCard G_RightLeg = new()
    {
        Id = "hd_g_rightleg",
        Name = "Enforcement Kick",
        Description = "A brutal kick that drives the opponent back into range for the next strike.",
        BodyPart = BodyPart.RightLeg,
        SatisfiesTags = new() { "leg", "right", "lower" },
        BasePower = 3,
        BaseDefense = 1,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 1,
        BaseMovementType = MovementType.Approach,
        MinRangeModifier = 0,
        MaxRangeModifier = 0,
        BaseCooldown = 1,
    };

    // ===== UNIQUE CARDS =====

    /// <summary>
    /// Collection Notice — fast opening strike to start stacking tokens early.
    /// Pairs with any upper body generic (arms, head, torso).
    /// </summary>
    public static readonly UniqueCard U_CollectionNotice = new()
    {
        Id = "hd_u_collectionnotice",
        Name = "Collection Notice",
        Description = "A swift, precise strike - the first reminder of what is owed.",
        RequiredBodyTags = new() { "upper" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Quickstep) },
        BasePower = 3,
        BaseDefense = 0,
        BaseSpeed = 2,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.Head,
        BaseCooldown = 2,
    };

    /// <summary>
    /// Overdue Penalty — a heavy strike with Bleed, building interest on the debt.
    /// Pairs with any arm generic.
    /// </summary>
    public static readonly UniqueCard U_OverduePenalty = new()
    {
        Id = "hd_u_overduepenalty",
        Name = "Overdue Penalty",
        Description = "A punishing blow that opens wounds - interest on an unpaid debt.",
        RequiredBodyTags = new() { "arm", "right" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Bleed, 1) },
        BasePower = 4,
        BaseDefense = 0,
        BaseSpeed = -1,
        MinMovement = 0,
        MaxMovement = 2,
        BaseMovementType = MovementType.Retreat,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.LeftArm,
        BaseCooldown = 2,
    };

    /// <summary>
    /// Enforcement Sweep — AoE-style Stagger on hit, disrupting the opponent's response.
    /// Pairs with torso or leg generics.
    /// </summary>
    public static readonly UniqueCard U_EnforcementSweep = new()
    {
        Id = "hd_u_enforcementsweep",
        Name = "Enforcement Sweep",
        Description = "A wide-arc sweep that staggers the target, buying time to collect.",
        RequiredBodyTags = new() { "lower" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Stagger) },
        BasePower = 2,
        BaseDefense = 1,
        BaseSpeed = 0,
        MinMovement = 1,
        MaxMovement = 2,
        BaseMovementType = MovementType.Free,
        MinRange = 1,
        MaxRange = 2,
        PrimaryTarget = Models.Fighter.BodyLocation.LeftLeg,
        SecondaryTarget = Models.Fighter.BodyLocation.RightLeg,
        BaseCooldown = 2,
    };

    /// <summary>
    /// Debt's Embrace — a pull attack at range, dragging the opponent back into melee.
    /// Pairs with right arm generic (matching Collector's Strike).
    /// </summary>
    public static readonly UniqueCard U_DebtsEmbrace = new()
    {
        Id = "hd_u_debtsembrace",
        Name = "Debt's Embrace",
        Description = "A long hooking strike that pulls the opponent within reach. No escape.",
        RequiredBodyTags = new() { "arm", "right" },
        Keywords = new() { new CardKeywordValue(CardKeyword.Pull) },
        BasePower = 3,
        BaseDefense = 0,
        BaseSpeed = 0,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Approach,
        MinRange = 2,
        MaxRange = 3,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.LeftArm,
        BaseCooldown = 2,
    };

    /// <summary>
    /// Final Reckoning — the finisher. Armour-breaking crushing blow for maximum damage.
    /// Pairs with any upper body generic.
    /// </summary>
    public static readonly UniqueCard U_FinalReckoning = new()
    {
        Id = "hd_u_finalreckoning",
        Name = "Final Reckoning",
        Description = "There is no appeal. A devastating finisher that shatters defence.",
        RequiredBodyTags = new() { "upper" },
        Keywords = new()
        {
            new CardKeywordValue(CardKeyword.ArmorBreak),
            new CardKeywordValue(CardKeyword.Crushing),
        },
        BasePower = 5,
        BaseDefense = 0,
        BaseSpeed = -2,
        MinMovement = 0,
        MaxMovement = 3,
        BaseMovementType = MovementType.Approach,
        MinRange = 1,
        MaxRange = 1,
        PrimaryTarget = Models.Fighter.BodyLocation.Torso,
        SecondaryTarget = Models.Fighter.BodyLocation.Core,
        BaseCooldown = 3,
    };

    // ===== FIGHTER DEFINITION =====

    public static FighterDefinition CreateDefinition() => new FighterDefinition
    {
        Id          = FighterId,
        Name        = "Honour Debt",
        Description = "A relentless collector. Gains speed from tokens on the opponent; tokens pay out double power.",
        Persona     = HonourDebtPersona.Instance,
        RankedUnlockWins = 10,
        GenericCards = new()
        {
            G_Head, G_Torso, G_LeftArm, G_RightArm, G_LeftLeg, G_RightLeg,
        },
        UniqueCards = new()
        {
            U_CollectionNotice,
            U_OverduePenalty,
            U_EnforcementSweep,
            U_DebtsEmbrace,
            U_FinalReckoning,
        },
        SpecialCards = new(),
    };
}
