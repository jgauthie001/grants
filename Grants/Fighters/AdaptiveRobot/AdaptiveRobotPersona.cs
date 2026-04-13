using Grants.Engine;
using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;

namespace Grants.Fighters.AdaptiveRobot;

/// <summary>
/// The Adaptive Robot persona.
///
/// ENERGY SYSTEM (max 5, starts at 3):
///   - Passively gains 1 energy at the start of each round's resolution.
///   - EMPTY (0 energy entering resolution): -2 Power, -2 Speed that round.
///   - OVERFLOW (gaining beyond 5): 2 random damage steps to self, energy caps at 5.
///
/// OPERATIONAL MODES (chosen each pre-round via PersonaChoiceRequest, must pick one):
///   assault    — +3 Power, -1 Defense, -1 Speed
///   shield     — +3 Defense, -2 Power
///   speed      — +3 Speed, -2 Defense
///   repair     — +1/+1/+1 all stats, gain 1 energy, attempt to heal 1 damage step
///   overcharge — +2/+2/+2 all stats, costs 2 energy upfront (fizzles if < 2 energy)
///
/// UNIQUE CARD ENERGY INTERACTIONS (applied in OnRoundResolutionStart):
///   bot_u_core_strike    — spend 2;  Assault bonus: +2 Power
///   bot_u_hydraulic_slam — spend 2;  Assault bonus: +2 Power
///   bot_u_targeting_lock — spend 1;  Assault bonus: +1 Power +1 Speed
///   bot_u_energy_barrier — spend 1;  Shield bonus:  +2 Defense
///   bot_u_overdrive_dash — spend 1;  Speed bonus:   +2 Speed
///   bot_u_self_repair    — spend 2;  Repair bonus:  extra +1 heal step
///   bot_u_arc_discharge  — spend 2;  (no mode bonus — ArmorBreak + Bleed already on card)
///   bot_u_kinetic_amp    — spend 1;  +1 Power per energy above 2 (pre-spend)
///   bot_u_emergency_vent — spend 0;  Shield bonus:  +2 Defense
///   bot_u_overload_strike— spend ALL; +2 Power per energy consumed
///   bot_u_magnetic_pull  — spend 1;  no mode bonus
///   bot_u_combat_protocol— spend 1;  Speed bonus:   +1 Speed
/// </summary>
public class AdaptiveRobotPersona : FighterPersona
{
    public static readonly AdaptiveRobotPersona Instance = new()
    {
        PersonaId   = "adaptive_robot",
        Name        = "Adaptive Robot",
        Description = "Manages an energy bank and cycles operational modes. Empty energy cripples output; overflow damages the robot itself.",
    };

    private AdaptiveRobotPersona() { }

    // ── Keys ────────────────────────────────────────────────────────────────
    private const string KeyEnergy = "bot_energy";
    private const string KeyMode   = "bot_mode";

    // ── Constants ────────────────────────────────────────────────────────────
    private const int MaxEnergy   = 5;
    private const int StartEnergy = 3;

    // ── Mode identifiers ─────────────────────────────────────────────────────
    private const string ModeAssault    = "assault";
    private const string ModeShield     = "shield";
    private const string ModeSpeed      = "speed";
    private const string ModeRepair     = "repair";
    private const string ModeOvercharge = "overcharge";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override PersonaState CreateRuntimeState()
    {
        var state = new PersonaState();
        state.Counters[KeyEnergy] = StartEnergy;
        state.CustomData["rng"]   = new Random();
        return state;
    }

    public override CardPair ModifyCardSelection(CardPair selectedPair, FighterInstance fighter, PersonaState state)
        => selectedPair;

    public override CardPair? GetPersonalizedAiDecision(
        FighterInstance ai, FighterInstance opponent, HexBoard board, PersonaState state)
        => null; // Use default AI card selection.

    // ── Pre-round mode selection ───────────────────────────────────────────────

    public override PersonaChoiceRequest? GetPreRoundSelfChoice(
        FighterInstance owner,
        FighterInstance opponent,
        MatchState match,
        PersonaState state)
    {
        int energy = state.Counters.GetValueOrDefault(KeyEnergy, StartEnergy);

        return new PersonaChoiceRequest
        {
            Prompt = $"Select operational mode for this round:  Energy {energy}/{MaxEnergy}",
            Options = new()
            {
                new PersonaChoiceOption(ModeAssault,    "Assault Mode",    "+3 Power, -1 Defense, -1 Speed"),
                new PersonaChoiceOption(ModeShield,     "Shield Mode",     "+3 Defense, -2 Power"),
                new PersonaChoiceOption(ModeSpeed,      "Speed Mode",      "+3 Speed, -2 Defense"),
                new PersonaChoiceOption(ModeRepair,     "Repair Mode",     "+1/+1/+1 all stats, gain 1 energy, chance to heal 1 step"),
                new PersonaChoiceOption(ModeOvercharge, "Overcharge Mode", "+2/+2/+2 all stats, costs 2 energy (fizzles if < 2)"),
            },
            CanSkip    = false,
            HeaderTint = (100, 200, 255),
        };
    }

    public override void OnPreRoundSelfChoiceSelected(
        FighterInstance owner, string? optionId, MatchState match, PersonaState state)
    {
        if (optionId != null)
            state.CustomData[KeyMode] = optionId;
        else
            state.CustomData.Remove(KeyMode);
    }

    public override string? ResolveAiPreRoundSelfChoice(
        FighterInstance owner,
        FighterInstance opponent,
        MatchState match,
        PersonaState state)
    {
        int energy   = state.Counters.GetValueOrDefault(KeyEnergy, StartEnergy);
        int damaged  = owner.LocationStates.Values.Count(ls => ls.State >= DamageState.Injured);
        int oppDisab = opponent.LocationStates.Values.Count(ls => ls.State == DamageState.Disabled);

        // Prioritise repair when badly hurt and have enough energy to still fight
        if (damaged >= 2 && energy >= 1)
            return ModeRepair;

        // Use overcharge when energy is plentiful
        if (energy >= 4)
            return ModeOvercharge;

        // Be aggressive when opponent is already damaged
        if (oppDisab >= 1 && energy >= 1)
            return ModeAssault;

        // At 0 energy: go defensive to avoid the empty penalty on top of a bad mode cost
        if (energy == 0)
            return ModeShield;

        // Default: alternate between assault and speed based on round
        return (match.CurrentRound % 2 == 0) ? ModeAssault : ModeSpeed;
    }

    // ── Round resolution ──────────────────────────────────────────────────────

    public override void OnRoundResolutionStart(
        RoundState round,
        MatchState match,
        FighterInstance ownerFighter,
        FighterInstance opponent,
        PersonaState state)
    {
        var rng    = GetRng(state);
        int energy = state.Counters.GetValueOrDefault(KeyEnergy, StartEnergy);

        // 1. Empty-energy penalty (checked before passive gain so full round context matters)
        if (energy == 0)
        {
            ownerFighter.RoundPowerModifier -= 2;
            ownerFighter.RoundSpeedModifier -= 2;
            round.Log.Add($"  [ROBOT] {ownerFighter.DisplayName} is out of energy! -2 Power, -2 Speed this round.");
        }

        // 2. Passive energy gain (+1 per round, may cause overflow)
        GainEnergy(ownerFighter, state, round, rng, 1);
        energy = state.Counters.GetValueOrDefault(KeyEnergy, 0);

        // 3. Apply chosen mode
        string mode = state.CustomData.TryGetValue(KeyMode, out var modeObj) ? (string)modeObj : "";
        state.CustomData.Remove(KeyMode);

        ApplyModeEffects(mode, energy, ownerFighter, state, round, rng);

        // 4. Unique-card energy cost and mode synergy
        var pair = ReferenceEquals(ownerFighter, match.FighterA) ? match.SelectedPairA : match.SelectedPairB;
        if (pair?.Unique != null)
            ApplyUniqueCardEnergyEffect(pair.Unique.Id, mode, ownerFighter, state, round, rng);
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    public override List<string> GetHudDisplayInfo(PersonaState state)
    {
        int energy = state.Counters.GetValueOrDefault(KeyEnergy, StartEnergy);
        string energyBar = new string('|', energy) + new string('.', MaxEnergy - energy);
        return new() { $"ROBOT | Energy [{energyBar}] {energy}/{MaxEnergy}" };
    }

    // ── Mode application ──────────────────────────────────────────────────────

    private static void ApplyModeEffects(
        string mode,
        int energyAfterPassive,
        FighterInstance owner,
        PersonaState state,
        RoundState round,
        Random rng)
    {
        switch (mode)
        {
            case ModeAssault:
                owner.RoundPowerModifier  += 3;
                owner.RoundDefenseModifier -= 1;
                owner.RoundSpeedModifier  -= 1;
                round.Log.Add($"  [ROBOT] Assault Mode: +3 Power, -1 Defense, -1 Speed.");
                break;

            case ModeShield:
                owner.RoundPowerModifier   -= 2;
                owner.RoundDefenseModifier += 3;
                round.Log.Add($"  [ROBOT] Shield Mode: +3 Defense, -2 Power.");
                break;

            case ModeSpeed:
                owner.RoundSpeedModifier   += 3;
                owner.RoundDefenseModifier -= 2;
                round.Log.Add($"  [ROBOT] Speed Mode: +3 Speed, -2 Defense.");
                break;

            case ModeRepair:
            {
                owner.RoundPowerModifier  += 1;
                owner.RoundDefenseModifier += 1;
                owner.RoundSpeedModifier  += 1;
                GainEnergy(owner, state, round, rng, 1);

                // Attempt to heal one damaged (not disabled) location
                var healable = owner.LocationStates.Values
                    .Where(ls => ls.State > DamageState.Healthy && ls.State < DamageState.Disabled)
                    .ToList();
                if (healable.Count > 0)
                {
                    var target = healable[rng.Next(healable.Count)];
                    target.ReduceDamage(1);
                    round.Log.Add($"  [ROBOT] Repair Mode: +1/+1/+1, +1 energy, repaired {target.Location} ({target.State}).");
                }
                else
                {
                    round.Log.Add($"  [ROBOT] Repair Mode: +1/+1/+1, +1 energy. (No damaged systems to repair.)");
                }
                break;
            }

            case ModeOvercharge:
            {
                int energy = state.Counters.GetValueOrDefault(KeyEnergy, 0);
                if (energy >= 2)
                {
                    state.Counters[KeyEnergy]  = energy - 2;
                    owner.RoundPowerModifier  += 2;
                    owner.RoundDefenseModifier += 2;
                    owner.RoundSpeedModifier  += 2;
                    round.Log.Add($"  [ROBOT] Overcharge Mode: -2 energy, +2/+2/+2. Energy: {state.Counters[KeyEnergy]}/{MaxEnergy}");
                }
                else
                {
                    round.Log.Add($"  [ROBOT] Overcharge Mode FIZZLED: only {energy} energy available (need 2).");
                }
                break;
            }

            // No mode selected (shouldn't happen with CanSkip=false, but handled defensively)
            default:
                break;
        }
    }

    // ── Unique card energy effects ─────────────────────────────────────────────

    private static void ApplyUniqueCardEnergyEffect(
        string cardId,
        string mode,
        FighterInstance owner,
        PersonaState state,
        RoundState round,
        Random rng)
    {
        int preSpend = state.Counters.GetValueOrDefault(KeyEnergy, 0);

        switch (cardId)
        {
            case "bot_u_core_strike":
                SpendEnergy(owner, state, round, 2);
                if (mode == ModeAssault)
                {
                    owner.RoundPowerModifier += 2;
                    round.Log.Add($"  [ROBOT] Power Core Strike + Assault Mode: +2 extra Power.");
                }
                break;

            case "bot_u_hydraulic_slam":
                SpendEnergy(owner, state, round, 2);
                if (mode == ModeAssault)
                {
                    owner.RoundPowerModifier += 2;
                    round.Log.Add($"  [ROBOT] Hydraulic Slam + Assault Mode: +2 extra Power.");
                }
                break;

            case "bot_u_targeting_lock":
                SpendEnergy(owner, state, round, 1);
                if (mode == ModeAssault)
                {
                    owner.RoundPowerModifier += 1;
                    owner.RoundSpeedModifier += 1;
                    round.Log.Add($"  [ROBOT] Targeting Lock + Assault Mode: +1 Power, +1 Speed.");
                }
                break;

            case "bot_u_energy_barrier":
                SpendEnergy(owner, state, round, 1);
                if (mode == ModeShield)
                {
                    owner.RoundDefenseModifier += 2;
                    round.Log.Add($"  [ROBOT] Energy Barrier + Shield Mode: +2 extra Defense.");
                }
                break;

            case "bot_u_overdrive_dash":
                SpendEnergy(owner, state, round, 1);
                if (mode == ModeSpeed)
                {
                    owner.RoundSpeedModifier += 2;
                    round.Log.Add($"  [ROBOT] Overdrive Dash + Speed Mode: +2 extra Speed.");
                }
                break;

            case "bot_u_self_repair":
            {
                SpendEnergy(owner, state, round, 2);
                // Base heal (always): attempt to repair 1 step from a damaged location
                var healable = owner.LocationStates.Values
                    .Where(ls => ls.State > DamageState.Healthy && ls.State < DamageState.Disabled)
                    .ToList();
                if (healable.Count > 0)
                {
                    var target = healable[rng.Next(healable.Count)];
                    target.ReduceDamage(1);
                    round.Log.Add($"  [ROBOT] Self-Repair Protocol: restored {target.Location} ({target.State}).");

                    // Repair Mode bonus: heal one more step
                    if (mode == ModeRepair)
                    {
                        var healable2 = owner.LocationStates.Values
                            .Where(ls => ls.State > DamageState.Healthy && ls.State < DamageState.Disabled)
                            .ToList();
                        if (healable2.Count > 0)
                        {
                            var target2 = healable2[rng.Next(healable2.Count)];
                            target2.ReduceDamage(1);
                            round.Log.Add($"  [ROBOT] Self-Repair + Repair Mode: extra restore on {target2.Location} ({target2.State})!");
                        }
                    }
                }
                else
                {
                    round.Log.Add($"  [ROBOT] Self-Repair Protocol: no damaged systems to repair.");
                }
                break;
            }

            case "bot_u_arc_discharge":
                // ArmorBreak + Bleed are on the card keywords. Energy spend enables the full effect.
                SpendEnergy(owner, state, round, 2);
                break;

            case "bot_u_kinetic_amp":
            {
                // Bonus based on energy BEFORE spending
                int bonus = Math.Max(0, preSpend - 2);
                SpendEnergy(owner, state, round, 1);
                if (bonus > 0)
                {
                    owner.RoundPowerModifier += bonus;
                    round.Log.Add($"  [ROBOT] Kinetic Amplifier: {preSpend} energy = +{bonus} bonus Power.");
                }
                break;
            }

            case "bot_u_emergency_vent":
                // Free to use (no energy cost). Parry is on the card keyword.
                if (mode == ModeShield)
                {
                    owner.RoundDefenseModifier += 2;
                    round.Log.Add($"  [ROBOT] Emergency Vent + Shield Mode: +2 extra Defense.");
                }
                break;

            case "bot_u_overload_strike":
            {
                // Spend ALL remaining energy; +2 Power per token consumed
                int spent = state.Counters.GetValueOrDefault(KeyEnergy, 0);
                state.Counters[KeyEnergy] = 0;
                if (spent > 0)
                {
                    int powerBonus = spent * 2;
                    owner.RoundPowerModifier += powerBonus;
                    round.Log.Add($"  [ROBOT] Overload Strike: spent {spent} energy = +{powerBonus} Power!");
                }
                else
                {
                    round.Log.Add($"  [ROBOT] Overload Strike: no energy to spend — 0 bonus Power.");
                }
                break;
            }

            case "bot_u_magnetic_pull":
                // Pull keyword is on the card. Energy cost only.
                SpendEnergy(owner, state, round, 1);
                break;

            case "bot_u_combat_protocol":
                SpendEnergy(owner, state, round, 1);
                if (mode == ModeSpeed)
                {
                    owner.RoundSpeedModifier += 1;
                    round.Log.Add($"  [ROBOT] Combat Protocol Shift + Speed Mode: +1 extra Speed.");
                }
                break;
        }
    }

    // ── Energy helpers ────────────────────────────────────────────────────────

    private static void GainEnergy(
        FighterInstance owner,
        PersonaState state,
        RoundState round,
        Random rng,
        int amount)
    {
        int current  = state.Counters.GetValueOrDefault(KeyEnergy, 0);
        int newValue = current + amount;

        if (newValue <= MaxEnergy)
        {
            state.Counters[KeyEnergy] = newValue;
            // Passive gain each round — not logged to avoid noise.
            // Explicit gains (Repair Mode) log themselves.
        }
        else
        {
            state.Counters[KeyEnergy] = MaxEnergy;
            round.Log.Add($"  [ROBOT] {owner.DisplayName}'s energy bank overloaded! 2 random damage steps!");

            var targetable = owner.LocationStates.Values
                .Where(ls => ls.State != DamageState.Disabled && ls.Location != BodyLocation.Stance)
                .ToList();
            if (targetable.Count == 0) return;

            for (int i = 0; i < 2; i++)
            {
                var loc = targetable[rng.Next(targetable.Count)];
                loc.ApplyDamage(1);
                round.Log.Add($"    {owner.DisplayName}'s {loc.Location} takes 1 overflow damage. ({loc.State})");
            }
        }
    }

    private static void SpendEnergy(
        FighterInstance owner,
        PersonaState state,
        RoundState round,
        int amount)
    {
        int current = state.Counters.GetValueOrDefault(KeyEnergy, 0);
        int spent   = Math.Min(current, amount);
        state.Counters[KeyEnergy] = current - spent;
        round.Log.Add($"  [ROBOT] {owner.DisplayName} spends {spent} energy. ({state.Counters[KeyEnergy]}/{MaxEnergy})");
    }

    private static Random GetRng(PersonaState state)
    {
        if (state.CustomData.TryGetValue("rng", out var r) && r is Random rng) return rng;
        var newRng = new Random();
        state.CustomData["rng"] = newRng;
        return newRng;
    }
}
