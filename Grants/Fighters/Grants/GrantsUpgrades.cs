using Grants.Models.Cards;
using Grants.Models.Upgrades;

namespace Grants.Fighters.Grants;

/// <summary>
/// Upgrade slot definitions for the Grants fighter.
/// 3 slots per card × 18 cards = 54 total slots.
/// Slot 0: 5 distinct matches → stat bonus (breadth reward)
/// Slot 1: 15 distinct matches → keyword (regular play reward)
/// Slot 2: mastery condition → keyword or persona unlock (mastery reward)
/// </summary>
public static class GrantsUpgrades
{
    public static FighterUpgradeDef Create()
    {
        var slots = new Dictionary<string, CardUpgradeSlotDef>();
        void Add(CardUpgradeSlotDef slot) => slots[slot.SlotId] = slot;

        // ── GENERICS ──────────────────────────────────────────────────────────────────

        // G_Head
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_Head.Id, SlotIndex = 0,
            Name = "Iron Forehead", Description = "Head strikes hit harder.",
            UpgradeType = SlotUpgradeType.PowerBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_Head.Id, SlotIndex = 1,
            Name = "Rattling Blow", Description = "Head Strike gains Stagger — disrupts opponent timing on hit.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Stagger, KeywordValue = 1,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_Head.Id, SlotIndex = 2,
            Name = "Snap Reaction", Description = "Head Strike gains Quickstep — fires +1 speed in the combined pool.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Quickstep, KeywordValue = 1,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.LandedHits, Target = 15,
                Description = "Land 15 hits with Head Strike",
            },
        });

        // G_Torso
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_Torso.Id, SlotIndex = 0,
            Name = "Thick Skin", Description = "Body Turn absorbs more punishment.",
            UpgradeType = SlotUpgradeType.DefenseBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_Torso.Id, SlotIndex = 1,
            Name = "Core Guard", Description = "Body Turn gains Guard — +2 defense when not attacking.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Guard, KeywordValue = 1,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_Torso.Id, SlotIndex = 2,
            Name = "Iron Gut", Description = "Body Turn gains MaxDamageCap 2 — torso cannot be advanced past Injured when this is played.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.MaxDamageCap, KeywordValue = 2,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.PlayedInMatches, Target = 20,
                Description = "Play Body Turn in 20 different matches",
            },
        });

        // G_LeftArm
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_LeftArm.Id, SlotIndex = 0,
            Name = "Faster Jab", Description = "Left jab fires quicker.",
            UpgradeType = SlotUpgradeType.SpeedBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_LeftArm.Id, SlotIndex = 1,
            Name = "Stinging Jab", Description = "Left Jab gains Quickstep — adds +1 to combined speed.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Quickstep, KeywordValue = 1,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_LeftArm.Id, SlotIndex = 2,
            Name = "Sting", Description = "Left Jab gains Bleed — on hit, target takes +1 damage next turn.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Bleed, KeywordValue = 1,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.LandedVsFaster, Target = 5,
                Description = "Land 5 Left Jabs against a faster opponent",
            },
        });

        // G_RightArm
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_RightArm.Id, SlotIndex = 0,
            Name = "Heavy Hand", Description = "Right cross hits harder.",
            UpgradeType = SlotUpgradeType.PowerBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_RightArm.Id, SlotIndex = 1,
            Name = "Armor Dent", Description = "Right Cross gains ArmorBreak — reduces target defense by 1 on hit.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.ArmorBreak, KeywordValue = 1,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_RightArm.Id, SlotIndex = 2,
            Name = "Extended Reach", Description = "Right Cross gains Lunge — +1 range but -1 defense on the turn.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Lunge, KeywordValue = 1,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.LandedAtRange, Target = 10, MinDistance = 3,
                Description = "Land 10 Right Cross hits from 3+ hexes away",
            },
        });

        // G_LeftLeg
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_LeftLeg.Id, SlotIndex = 0,
            Name = "Light Feet", Description = "Left step moves faster.",
            UpgradeType = SlotUpgradeType.SpeedBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_LeftLeg.Id, SlotIndex = 1,
            Name = "Ghost Foot", Description = "Left Step gains Sidestep — allows diagonal movement.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Sidestep, KeywordValue = 1,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_LeftLeg.Id, SlotIndex = 2,
            Name = "Safe Exit", Description = "Left Step gains Disengage 1 — retreat 1 hex when out of range or missing.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Disengage, KeywordValue = 1,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.PlayedInMatches, Target = 20,
                Description = "Play Left Step in 20 different matches",
            },
        });

        // G_RightLeg
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_RightLeg.Id, SlotIndex = 0,
            Name = "Powerful Kick", Description = "Right kick hits harder.",
            UpgradeType = SlotUpgradeType.PowerBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_RightLeg.Id, SlotIndex = 1,
            Name = "Knockdown Kick", Description = "Right Kick gains Knockback — pushes opponent 1 hex on hit.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Knockback, KeywordValue = 1,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_RightLeg.Id, SlotIndex = 2,
            Name = "Follow-Up Step", Description = "Right Kick gains FollowThrough 1 — advance 1 hex after a landed hit.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.FollowThrough, KeywordValue = 1,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.LandedHits, Target = 20,
                Description = "Land 20 hits with Right Kick",
            },
        });

        // G_Core
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_Core.Id, SlotIndex = 0,
            Name = "Reinforced Core", Description = "Center Drive absorbs more punishment.",
            UpgradeType = SlotUpgradeType.DefenseBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_Core.Id, SlotIndex = 1,
            Name = "Magnetic Pull", Description = "Center Drive gains Pull — drag opponent 1 hex toward you on hit.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Pull, KeywordValue = 1,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_Core.Id, SlotIndex = 2,
            Name = "Bone Crusher", Description = "Center Drive gains Crushing — advances damage state one extra step on hit.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Crushing, KeywordValue = 1,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.LandedHits, Target = 15,
                Description = "Land 15 hits with Center Drive",
            },
        });

        // G_Stance
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_Stance.Id, SlotIndex = 0,
            Name = "Quick Feet", Description = "Footwork movement speed improves.",
            UpgradeType = SlotUpgradeType.SpeedBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_Stance.Id, SlotIndex = 1,
            Name = "Ghost Step", Description = "Footwork gains Quickstep — adds +1 to combined speed.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Quickstep, KeywordValue = 1,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.G_Stance.Id, SlotIndex = 2,
            Name = "Threat Range", Description = "Footwork extends max range by 1 — your threat zone grows.",
            UpgradeType = SlotUpgradeType.RangeExtension, StatBonus = 1,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.WonMatchWithCard, Target = 10,
                Description = "Win 10 matches with Footwork played",
            },
        });

        // ── UNIQUES ──────────────────────────────────────────────────────────────────

        // U_Haymaker
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_Haymaker.Id, SlotIndex = 0,
            Name = "Heavier Swing", Description = "Haymaker hits even harder.",
            UpgradeType = SlotUpgradeType.PowerBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_Haymaker.Id, SlotIndex = 1,
            Name = "Bone-Breaking Blow", Description = "Haymaker gains Crushing — advances damage state one extra step.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Crushing, KeywordValue = 1,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_Haymaker.Id, SlotIndex = 2,
            Name = "Overcommit", Description = "Haymaker gains FollowThrough 1 — advance 1 hex after a landed hit to keep pressing.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.FollowThrough, KeywordValue = 1,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.LandedVsFaster, Target = 8,
                Description = "Land 8 Haymakers against a faster opponent",
            },
        });

        // U_Clinch
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_Clinch.Id, SlotIndex = 0,
            Name = "Tighter Grip", Description = "Clinch is harder to slip out of.",
            UpgradeType = SlotUpgradeType.DefenseBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_Clinch.Id, SlotIndex = 1,
            Name = "Momentum Break", Description = "Clinch gains Disrupt — on hit, cancels opponent's unique card choice this turn.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Disrupt, KeywordValue = 1,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_Clinch.Id, SlotIndex = 2,
            Name = "Into the Pocket", Description = "Clinch gains Pull — drag opponent 1 hex closer on hit.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Pull, KeywordValue = 1,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.PlayedInMatches, Target = 20,
                Description = "Play Clinch in 20 different matches",
            },
        });

        // U_CrossCounter
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_CrossCounter.Id, SlotIndex = 0,
            Name = "Counter Timing", Description = "Cross Counter fires faster.",
            UpgradeType = SlotUpgradeType.SpeedBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_CrossCounter.Id, SlotIndex = 1,
            Name = "Exposed Strike", Description = "Cross Counter gains ArmorBreak — exploits the opening created by the slip.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.ArmorBreak, KeywordValue = 1,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_CrossCounter.Id, SlotIndex = 2,
            Name = "Through the Gap", Description = "Cross Counter gains Piercing — ignores half of defender's defense stat.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Piercing, KeywordValue = 1,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.WonMatchWithCard, Target = 5,
                Description = "Win 5 matches with Cross Counter played",
            },
        });

        // U_BullRush
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_BullRush.Id, SlotIndex = 0,
            Name = "Stampede", Description = "Bull Rush hits harder.",
            UpgradeType = SlotUpgradeType.PowerBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_BullRush.Id, SlotIndex = 1,
            Name = "Keep Moving", Description = "Bull Rush FollowThrough upgraded to 2 — press 2 hexes forward on a landed hit.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.FollowThrough, KeywordValue = 2,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_BullRush.Id, SlotIndex = 2,
            Name = "Battering Ram", Description = "Bull Rush gains extra Knockback — hit and push them further out of position.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Knockback, KeywordValue = 2,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.EventCounter, Target = 10, CounterKey = "follow_through",
                Description = "Trigger FollowThrough 10 times across all matches",
            },
        });

        // U_LowSweep
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_LowSweep.Id, SlotIndex = 0,
            Name = "Faster Sweep", Description = "Low Sweep fires quicker.",
            UpgradeType = SlotUpgradeType.SpeedBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_LowSweep.Id, SlotIndex = 1,
            Name = "Lacerate", Description = "Low Sweep gains Bleed — target takes +1 damage next turn on hit.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Bleed, KeywordValue = 1,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_LowSweep.Id, SlotIndex = 2,
            Name = "Deep Exit", Description = "Low Sweep Disengage upgraded to 2 — retreat 2 hexes after missing.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Disengage, KeywordValue = 2,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.LandedHits, Target = 15,
                Description = "Land 15 hits with Low Sweep",
            },
        });

        // U_Overhand
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_Overhand.Id, SlotIndex = 0,
            Name = "Heavier Loop", Description = "Overhand hits harder.",
            UpgradeType = SlotUpgradeType.PowerBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_Overhand.Id, SlotIndex = 1,
            Name = "Committed Swing", Description = "Overhand Recoil upgraded to 2 — the weight of the blow snaps you further back.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Recoil, KeywordValue = 2,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_Overhand.Id, SlotIndex = 2,
            Name = "Apex Overhand",
            Description = "Persona unlock: when Overhand lands on a target with 0 defense on that location, the next attack this match ignores all defense.",
            UpgradeType = SlotUpgradeType.PersonaUnlock, PersonaUnlockId = "overhand_apex",
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.LandedVsFaster, Target = 5,
                Description = "Land 5 Overhands against a faster opponent",
            },
        });

        // U_SideStep
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_SideStep.Id, SlotIndex = 0,
            Name = "Fluid Dodge", Description = "Sidestep defense improves.",
            UpgradeType = SlotUpgradeType.DefenseBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_SideStep.Id, SlotIndex = 1,
            Name = "Redirect", Description = "Sidestep gains Deflect — when hit, redirect half damage to another location.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Deflect, KeywordValue = 1,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_SideStep.Id, SlotIndex = 2,
            Name = "Matador Counter", Description = "Sidestep gains Parry — if opponent attacks the same body part, counter at +1 power.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Parry, KeywordValue = 1,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.PlayedInMatches, Target = 25,
                Description = "Play Sidestep in 25 different matches",
            },
        });

        // U_BodyShot
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_BodyShot.Id, SlotIndex = 0,
            Name = "Gut Punch", Description = "Body Shot hits harder.",
            UpgradeType = SlotUpgradeType.PowerBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_BodyShot.Id, SlotIndex = 1,
            Name = "Deep Bruise", Description = "Body Shot Bleed upgraded to 2 — target takes +2 damage next turn.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Bleed, KeywordValue = 2,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.U_BodyShot.Id, SlotIndex = 2,
            Name = "Wind Them", Description = "Body Shot gains Stagger — on hit, opponent's cooldowns all increase by 1 next turn.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Stagger, KeywordValue = 1,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.LandedHits, Target = 20,
                Description = "Land 20 hits with Body Shot",
            },
        });

        // ── SPECIALS ──────────────────────────────────────────────────────────────────

        // S_Obliterator
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.S_Obliterator.Id, SlotIndex = 0,
            Name = "Finisher Form", Description = "Obliterator hits even harder.",
            UpgradeType = SlotUpgradeType.PowerBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.S_Obliterator.Id, SlotIndex = 1,
            Name = "Relentless", Description = "Obliterator cooldown reduced by 1 — it comes back sooner.",
            UpgradeType = SlotUpgradeType.CooldownReduction, CooldownReduction = 1,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.S_Obliterator.Id, SlotIndex = 2,
            Name = "Death Sentence", Description = "Obliterator range extended by 1 — can now reach Close range, not just Adjacent.",
            UpgradeType = SlotUpgradeType.RangeExtension, StatBonus = 1,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.WonWithKillingBlow, Target = 3,
                Description = "Win 3 matches where Obliterator delivered the killing blow",
            },
        });

        // S_BerserkRush
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.S_BerserkRush.Id, SlotIndex = 0,
            Name = "Speed Demon", Description = "Berserk Rush comes out faster.",
            UpgradeType = SlotUpgradeType.SpeedBonus, StatBonus = 1,
            DistinctMatchesRequired = 5,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.S_BerserkRush.Id, SlotIndex = 1,
            Name = "Surge", Description = "Berserk Rush Lunge upgraded to 2 — +2 range, -2 defense on use.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Lunge, KeywordValue = 2,
            DistinctMatchesRequired = 15,
        });
        Add(new CardUpgradeSlotDef
        {
            CardId = GrantsFighter.S_BerserkRush.Id, SlotIndex = 2,
            Name = "Momentum Cascade", Description = "Berserk Rush gains FollowThrough 2 — keep pushing 2 hexes forward on a landed hit.",
            UpgradeType = SlotUpgradeType.AddKeyword, KeywordAdded = CardKeyword.FollowThrough, KeywordValue = 2,
            Mastery = new MasteryCondition
            {
                Type = MasteryConditionType.LandedHits, Target = 10,
                Description = "Land 10 hits with Berserk Rush",
            },
        });

        return new FighterUpgradeDef
        {
            FighterId = GrantsFighter.FighterId,
            Slots = slots,
        };
    }
}
