using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Upgrades;

namespace Grants.Fighters.Grants;

/// <summary>
/// Grants' upgrade tree. Four branches: Offense, Toughness, Pressure, Finisher.
/// 36 card upgrade slots + 4 items + 4 final nodes = ~44 total upgrades.
/// All Name/Description strings marked _pl for content writing pass.
/// </summary>
public static class GrantsUpgradeTree
{
    public static UpgradeTree Create()
    {
        var nodes = new Dictionary<string, UpgradeNode>();
        var branches = new Dictionary<string, List<string>>();

        // ===== OFFENSE BRANCH =====
        // Focus: Right Arm, Haymaker, Overhand, Obliterator
        var offense = new List<string>();

        Add(nodes, new UpgradeNode
        {
            Id = "g_off_1", Name = "Heavy Hand_pl",
            Description = "Your right arm strikes hit harder._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Offense",
            TargetCardId = GrantsFighter.G_RightArm.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.PowerBonus, StatBonus = 1 },
        });
        offense.Add("g_off_1");

        Add(nodes, new UpgradeNode
        {
            Id = "g_off_2", Name = "Iron Wrist_pl",
            Description = "Your right arm becomes more resilient under damage._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Offense",
            Prerequisites = new() { "g_off_1" },
            TargetCardId = GrantsFighter.G_RightArm.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.DefenseBonus, StatBonus = 1 },
        });
        offense.Add("g_off_2");

        Add(nodes, new UpgradeNode
        {
            Id = "g_off_3", Name = "Windmill_pl",
            Description = "The Haymaker gains more power at the cost of its already low speed._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 2, Branch = "Offense",
            Prerequisites = new() { "g_off_2" },
            TargetCardId = GrantsFighter.U_Haymaker.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.PowerBonus, StatBonus = 2 },
        });
        offense.Add("g_off_3");

        Add(nodes, new UpgradeNode
        {
            Id = "g_off_4", Name = "Practiced Swing_pl",
            Description = "The Haymaker comes out slightly faster._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Offense",
            Prerequisites = new() { "g_off_3" },
            TargetCardId = GrantsFighter.U_Haymaker.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.SpeedBonus, StatBonus = 1 },
        });
        offense.Add("g_off_4");

        Add(nodes, new UpgradeNode
        {
            Id = "g_off_5", Name = "Overhand Pro_pl",
            Description = "The Overhand now tears through defenses even harder._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Offense",
            Prerequisites = new() { "g_off_2" },
            TargetCardId = GrantsFighter.U_Overhand.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.PowerBonus, StatBonus = 1 },
        });
        offense.Add("g_off_5");

        Add(nodes, new UpgradeNode
        {
            Id = "g_off_6", Name = "Shattering Blow_pl",
            Description = "Overhand now has the Crushing keyword — advances damage state further._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 2, PowerRatingValue = 2, Branch = "Offense",
            Prerequisites = new() { "g_off_5" },
            TargetCardId = GrantsFighter.U_Overhand.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Crushing },
        });
        offense.Add("g_off_6");

        // Item in Offense branch
        Add(nodes, new UpgradeNode
        {
            Id = "g_off_item1", Name = "Weighted Wraps_pl",
            Description = "Passive: all arm card attacks deal +1 power, but arm speed reduced by 1._pl",
            NodeType = UpgradeNodeType.Item, Cost = 2, PowerRatingValue = 2, Branch = "Offense",
            Prerequisites = new() { "g_off_4" },
            ItemId = "grants_item_weighted_wraps",
            ItemEffect = new() { BodyTagPowerBonus = "arm", BodyTagPowerValue = 1, BodyTagSpeedBonus = "arm", BodyTagSpeedValue = -1 },
        });
        offense.Add("g_off_item1");

        // Obliterator upgrades
        Add(nodes, new UpgradeNode
        {
            Id = "g_off_7", Name = "Finisher Form_pl",
            Description = "The Obliterator gains +1 power._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 2, PowerRatingValue = 2, Branch = "Offense",
            Prerequisites = new() { "g_off_6", "g_off_item1" },
            TargetCardId = GrantsFighter.S_Obliterator.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.PowerBonus, StatBonus = 1 },
        });
        offense.Add("g_off_7");

        Add(nodes, new UpgradeNode
        {
            Id = "g_off_8", Name = "Death Sentence_pl",
            Description = "The Obliterator can now be used from Close range, not just Adjacent._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 2, PowerRatingValue = 2, Branch = "Offense",
            Prerequisites = new() { "g_off_7" },
            TargetCardId = GrantsFighter.S_Obliterator.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.RangeExtension, StatBonus = 1 },
        });
        offense.Add("g_off_8");

        branches["Offense"] = offense;

        // ===== TOUGHNESS BRANCH =====
        // Focus: Torso, Head, Core, Cross Counter
        var tough = new List<string>();

        Add(nodes, new UpgradeNode
        {
            Id = "g_tgh_1", Name = "Thick Skin_pl",
            Description = "Your torso shrugs off a bit more damage._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Toughness",
            TargetCardId = GrantsFighter.G_Torso.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.DefenseBonus, StatBonus = 1 },
        });
        tough.Add("g_tgh_1");

        Add(nodes, new UpgradeNode
        {
            Id = "g_tgh_2", Name = "Stone Core_pl",
            Description = "Torso block speed improves slightly._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Toughness",
            Prerequisites = new() { "g_tgh_1" },
            TargetCardId = GrantsFighter.G_Torso.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.SpeedBonus, StatBonus = 1 },
        });
        tough.Add("g_tgh_2");

        Add(nodes, new UpgradeNode
        {
            Id = "g_tgh_3", Name = "Iron Chin_pl",
            Description = "Head block gains +1 defense._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Toughness",
            Prerequisites = new() { "g_tgh_1" },
            TargetCardId = GrantsFighter.G_Head.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.DefenseBonus, StatBonus = 1 },
        });
        tough.Add("g_tgh_3");

        Add(nodes, new UpgradeNode
        {
            Id = "g_tgh_4", Name = "Hard Head_pl",
            Description = "Head card cooldown reduced to 0 — can use it back to back._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 2, PowerRatingValue = 2, Branch = "Toughness",
            Prerequisites = new() { "g_tgh_3" },
            TargetCardId = GrantsFighter.G_Head.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.CooldownReduction, CooldownReduction = 1 },
        });
        tough.Add("g_tgh_4");

        Add(nodes, new UpgradeNode
        {
            Id = "g_tgh_5", Name = "Counterweight_pl",
            Description = "Cross Counter gains +1 power._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Toughness",
            Prerequisites = new() { "g_tgh_2" },
            TargetCardId = GrantsFighter.U_CrossCounter.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.PowerBonus, StatBonus = 1 },
        });
        tough.Add("g_tgh_5");

        Add(nodes, new UpgradeNode
        {
            Id = "g_tgh_6", Name = "Split Second_pl",
            Description = "Cross Counter fires even faster._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 2, Branch = "Toughness",
            Prerequisites = new() { "g_tgh_5" },
            TargetCardId = GrantsFighter.U_CrossCounter.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.SpeedBonus, StatBonus = 1 },
        });
        tough.Add("g_tgh_6");

        // Item in Toughness branch
        Add(nodes, new UpgradeNode
        {
            Id = "g_tgh_item1", Name = "Knee Brace_pl",
            Description = "Passive: left leg cannot be Disabled — maximum damage state is Injured._pl",
            NodeType = UpgradeNodeType.Item, Cost = 2, PowerRatingValue = 2, Branch = "Toughness",
            Prerequisites = new() { "g_tgh_4" },
            ItemId = "grants_item_knee_brace",
            ItemEffect = new() { DamageCapLocation = BodyLocation.LeftLeg, DamageCap = DamageState.Injured },
        });
        tough.Add("g_tgh_item1");

        Add(nodes, new UpgradeNode
        {
            Id = "g_tgh_7", Name = "Fortified Core_pl",
            Description = "Core card gains +1 defense._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Toughness",
            Prerequisites = new() { "g_tgh_2" },
            TargetCardId = GrantsFighter.G_Core.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.DefenseBonus, StatBonus = 1 },
        });
        tough.Add("g_tgh_7");

        Add(nodes, new UpgradeNode
        {
            Id = "g_tgh_8", Name = "Pressure Wall_pl",
            Description = "Core card cooldown reduced to 0._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 2, PowerRatingValue = 2, Branch = "Toughness",
            Prerequisites = new() { "g_tgh_7" },
            TargetCardId = GrantsFighter.G_Core.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.CooldownReduction, CooldownReduction = 1 },
        });
        tough.Add("g_tgh_8");

        branches["Toughness"] = tough;

        // ===== PRESSURE BRANCH =====
        // Focus: Stance, Left Leg, Bull Rush, Body Shot
        var pressure = new List<string>();

        Add(nodes, new UpgradeNode
        {
            Id = "g_prs_1", Name = "Quick Feet_pl",
            Description = "Footwork card moves one extra hex._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Pressure",
            TargetCardId = GrantsFighter.G_Stance.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.MovementBonus, StatBonus = 1 },
        });
        pressure.Add("g_prs_1");

        Add(nodes, new UpgradeNode
        {
            Id = "g_prs_2", Name = "Ghost Step_pl",
            Description = "Footwork gains the Sidestep keyword._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 2, Branch = "Pressure",
            Prerequisites = new() { "g_prs_1" },
            TargetCardId = GrantsFighter.G_Stance.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Sidestep },
        });
        pressure.Add("g_prs_2");

        Add(nodes, new UpgradeNode
        {
            Id = "g_prs_3", Name = "Stampede_pl",
            Description = "Bull Rush gains +1 movement._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Pressure",
            Prerequisites = new() { "g_prs_1" },
            TargetCardId = GrantsFighter.U_BullRush.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.MovementBonus, StatBonus = 1 },
        });
        pressure.Add("g_prs_3");

        Add(nodes, new UpgradeNode
        {
            Id = "g_prs_4", Name = "Battering Ram_pl",
            Description = "Bull Rush gains the Crushing keyword._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 2, PowerRatingValue = 2, Branch = "Pressure",
            Prerequisites = new() { "g_prs_3" },
            TargetCardId = GrantsFighter.U_BullRush.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Crushing },
        });
        pressure.Add("g_prs_4");

        Add(nodes, new UpgradeNode
        {
            Id = "g_prs_5", Name = "Gut Feeling_pl",
            Description = "Body Shot gains +1 power._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Pressure",
            Prerequisites = new() { "g_prs_1" },
            TargetCardId = GrantsFighter.U_BodyShot.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.PowerBonus, StatBonus = 1 },
        });
        pressure.Add("g_prs_5");

        Add(nodes, new UpgradeNode
        {
            Id = "g_prs_6", Name = "Relentless_pl",
            Description = "Body Shot cooldown reduced to 1 turn._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 2, PowerRatingValue = 2, Branch = "Pressure",
            Prerequisites = new() { "g_prs_5" },
            TargetCardId = GrantsFighter.U_BodyShot.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.CooldownReduction, CooldownReduction = 1 },
        });
        pressure.Add("g_prs_6");

        // Item in Pressure branch
        Add(nodes, new UpgradeNode
        {
            Id = "g_prs_item1", Name = "Adrenaline Shot_pl",
            Description = "Passive: first time any location reaches Injured, gain +2 speed on all cards for 1 turn._pl",
            NodeType = UpgradeNodeType.Item, Cost = 2, PowerRatingValue = 2, Branch = "Pressure",
            Prerequisites = new() { "g_prs_4" },
            ItemId = "grants_item_adrenaline",
            ItemEffect = new() { AdrenalineOnFirstInjury = true, AdrenalineSpeedBonus = 2, AdrenalineTurns = 1 },
        });
        pressure.Add("g_prs_item1");

        Add(nodes, new UpgradeNode
        {
            Id = "g_prs_7", Name = "Speed Demon_pl",
            Description = "Berserk Rush gains +1 speed._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 2, PowerRatingValue = 2, Branch = "Pressure",
            Prerequisites = new() { "g_prs_4", "g_prs_6" },
            TargetCardId = GrantsFighter.S_BerserkRush.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.SpeedBonus, StatBonus = 1 },
        });
        pressure.Add("g_prs_7");

        Add(nodes, new UpgradeNode
        {
            Id = "g_prs_8", Name = "Surge_pl",
            Description = "Berserk Rush cooldown reduced to 2 turns._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 2, PowerRatingValue = 2, Branch = "Pressure",
            Prerequisites = new() { "g_prs_7" },
            TargetCardId = GrantsFighter.S_BerserkRush.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.CooldownReduction, CooldownReduction = 1 },
        });
        pressure.Add("g_prs_8");

        branches["Pressure"] = pressure;

        // ===== FINISHER BRANCH =====
        // Focus: Left Arm, Low Sweep, Clinch, remaining cards
        var finisher = new List<string>();

        Add(nodes, new UpgradeNode
        {
            Id = "g_fin_1", Name = "Lead Hand_pl",
            Description = "Left jab becomes faster._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Finisher",
            TargetCardId = GrantsFighter.G_LeftArm.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.SpeedBonus, StatBonus = 1 },
        });
        finisher.Add("g_fin_1");

        Add(nodes, new UpgradeNode
        {
            Id = "g_fin_2", Name = "Sting_pl",
            Description = "Left jab gains the Bleed keyword._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 2, Branch = "Finisher",
            Prerequisites = new() { "g_fin_1" },
            TargetCardId = GrantsFighter.G_LeftArm.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Bleed },
        });
        finisher.Add("g_fin_2");

        Add(nodes, new UpgradeNode
        {
            Id = "g_fin_3", Name = "Sweep Artist_pl",
            Description = "Low Sweep gains +1 speed._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Finisher",
            Prerequisites = new() { "g_fin_1" },
            TargetCardId = GrantsFighter.U_LowSweep.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.SpeedBonus, StatBonus = 1 },
        });
        finisher.Add("g_fin_3");

        Add(nodes, new UpgradeNode
        {
            Id = "g_fin_4", Name = "Ankle Breaker_pl",
            Description = "Low Sweep gains the Stagger keyword._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 2, PowerRatingValue = 2, Branch = "Finisher",
            Prerequisites = new() { "g_fin_3" },
            TargetCardId = GrantsFighter.U_LowSweep.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Stagger },
        });
        finisher.Add("g_fin_4");

        Add(nodes, new UpgradeNode
        {
            Id = "g_fin_5", Name = "Bear Hug_pl",
            Description = "Clinch gains +1 defense._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Finisher",
            Prerequisites = new() { "g_fin_2" },
            TargetCardId = GrantsFighter.U_Clinch.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.DefenseBonus, StatBonus = 1 },
        });
        finisher.Add("g_fin_5");

        Add(nodes, new UpgradeNode
        {
            Id = "g_fin_6", Name = "Death Grip_pl",
            Description = "Clinch cooldown reduced to 1 turn._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 2, PowerRatingValue = 2, Branch = "Finisher",
            Prerequisites = new() { "g_fin_5" },
            TargetCardId = GrantsFighter.U_Clinch.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.CooldownReduction, CooldownReduction = 1 },
        });
        finisher.Add("g_fin_6");

        // Item in Finisher branch
        Add(nodes, new UpgradeNode
        {
            Id = "g_fin_item1", Name = "Grudge Match_pl",
            Description = "Passive: if any critical location (Head/Torso) reaches Injured, all unique card power +1 for the rest of the match._pl",
            NodeType = UpgradeNodeType.Item, Cost = 2, PowerRatingValue = 2, Branch = "Finisher",
            Prerequisites = new() { "g_fin_4", "g_fin_6" },
            ItemId = "grants_item_grudge_match",
            ItemEffect = new(), // Logic handled in ResolutionEngine via ActiveItemIds check
        });
        finisher.Add("g_fin_item1");

        Add(nodes, new UpgradeNode
        {
            Id = "g_fin_7", Name = "Sidestep Pro_pl",
            Description = "Sidestep gains +1 defense._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 1, PowerRatingValue = 1, Branch = "Finisher",
            Prerequisites = new() { "g_fin_2" },
            TargetCardId = GrantsFighter.U_SideStep.Id, SlotIndex = 0,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.DefenseBonus, StatBonus = 1 },
        });
        finisher.Add("g_fin_7");

        Add(nodes, new UpgradeNode
        {
            Id = "g_fin_8", Name = "Counter Launch_pl",
            Description = "Sidestep gains the Press keyword — after a successful dodge, advance 1 hex._pl",
            NodeType = UpgradeNodeType.CardSlot, Cost = 2, PowerRatingValue = 2, Branch = "Finisher",
            Prerequisites = new() { "g_fin_7" },
            TargetCardId = GrantsFighter.U_SideStep.Id, SlotIndex = 1,
            UpgradeEffect = new() { IsUnlocked = true, UpgradeType = CardUpgradeType.AddKeyword, KeywordAdded = CardKeyword.Press },
        });
        finisher.Add("g_fin_8");

        branches["Finisher"] = finisher;

        // ===== FINAL NODES (4) =====
        // Each requires completing two full branches

        Add(nodes, new UpgradeNode
        {
            Id = "g_final_ironclad", Name = "Ironclad_pl",
            Description = "Your Torso location can never be Disabled — damage caps at Injured._pl",
            NodeType = UpgradeNodeType.FinalNode, Cost = 3, PowerRatingValue = 3, Branch = "Final",
            Prerequisites = new() { "g_tgh_8", "g_off_8" },
            FinalEffect = new()
            {
                FlavorDescription = "Some men wear down. Grants just wears through._pl",
                Effect = new() { DamageCapLocation = BodyLocation.Torso, DamageCap = DamageState.Injured },
            },
        });

        Add(nodes, new UpgradeNode
        {
            Id = "g_final_relentlessrush", Name = "Relentless Rush_pl",
            Description = "After any successful hit, movement generic cooldowns are refunded._pl",
            NodeType = UpgradeNodeType.FinalNode, Cost = 3, PowerRatingValue = 3, Branch = "Final",
            Prerequisites = new() { "g_prs_8", "g_tgh_8" },
            FinalEffect = new()
            {
                FlavorDescription = "He doesn't stop. He doesn't pause. He doesn't give you a moment to breathe._pl",
                MovementCooldownRefundOnSpeedWin = true,
            },
        });

        Add(nodes, new UpgradeNode
        {
            Id = "g_final_bloodlust", Name = "Bloodlust_pl",
            Description = "On a simultaneous hit where you win on power, splash half damage to a second random location._pl",
            NodeType = UpgradeNodeType.FinalNode, Cost = 3, PowerRatingValue = 3, Branch = "Final",
            Prerequisites = new() { "g_off_8", "g_fin_item1" },
            FinalEffect = new()
            {
                FlavorDescription = "A hit lands — and something else breaks too._pl",
                SplashDamageOnSimultaneousPowerWin = true,
            },
        });

        Add(nodes, new UpgradeNode
        {
            Id = "g_final_deathblow", Name = "Death Blow_pl",
            Description = "Both special cards have their cooldown permanently reduced to 2 turns._pl",
            NodeType = UpgradeNodeType.FinalNode, Cost = 3, PowerRatingValue = 3, Branch = "Final",
            Prerequisites = new() { "g_prs_8", "g_off_8" },
            FinalEffect = new()
            {
                FlavorDescription = "When Grants reaches back for the final blow, there is no avoiding it._pl",
                SpecialCooldownReduction = 1,
            },
        });

        var finalNodeIds = new List<string> { "g_final_ironclad", "g_final_relentlessrush", "g_final_bloodlust", "g_final_deathblow" };
        branches["Final"] = finalNodeIds;

        return new UpgradeTree
        {
            FighterId = GrantsFighter.FighterId,
            Nodes = nodes,
            Branches = branches,
            FinalNodeIds = finalNodeIds,
        };
    }

    private static void Add(Dictionary<string, UpgradeNode> dict, UpgradeNode node) =>
        dict[node.Id] = node;
}
