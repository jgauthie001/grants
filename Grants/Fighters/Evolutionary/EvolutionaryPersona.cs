using Grants.Engine;
using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;

namespace Grants.Fighters.Evolutionary;

/// <summary>
/// The Evolutionary persona. Before every card selection the Evolutionary
/// chooses one of five evolution cards. That evolution's power, defense, and
/// speed are added to the fighter's round modifiers, effectively giving them
/// three cards worth of stats (Generic + Unique + Evolution) every round.
///
/// Unlike the Mutator, the evolution choice is ALWAYS available and MANDATORY
/// - there are no cooldowns involved. All five evolutions are on the table
/// every round; the decision is about which direction best suits the situation.
/// </summary>
public class EvolutionaryPersona : FighterPersona
{
    public static readonly EvolutionaryPersona Instance = new()
    {
        PersonaId   = "evolutionary",
        Name        = "Evolutionary",
        Description = "Picks one of five evolutions each round to empower their action. Mandatory, always available, and highly adaptive.",
    };

    private const string ChosenKey = "evolutionary_chosen_card";

    private EvolutionaryPersona() { }

    public override PersonaState CreateRuntimeState() => new PersonaState();

    // ---- Pre-round self-choice (generic protocol) ----

    public override PersonaChoiceRequest? GetPreRoundSelfChoice(
        FighterInstance owner, FighterInstance opponent, MatchState match, PersonaState state)
    {
        var evos = owner.Definition.EvolutionCards;
        if (evos.Count == 0) return null;

        var options = evos.Select(ev =>
        {
            string spdStr = ev.BaseSpeed >= 0 ? $"+{ev.BaseSpeed}" : $"{ev.BaseSpeed}";
            return new PersonaChoiceOption(
                ev.Id,
                $"{ev.Name}  Pwr:+{ev.BasePower} Def:+{ev.BaseDefense} Spd:{spdStr}",
                ev.Description);
        }).ToList();

        return new PersonaChoiceRequest
        {
            Prompt     = "Pick the evolution that will empower your action this round (mandatory):",
            Options    = options,
            CanSkip    = false,
            HeaderTint = (80, 200, 220),
        };
    }

    public override void OnPreRoundSelfChoiceSelected(
        FighterInstance owner, string? optionId, MatchState match, PersonaState state)
    {
        if (optionId == null)
            state.CustomData.Remove(ChosenKey);
        else
            state.CustomData[ChosenKey] = optionId;
    }

    public override string? ResolveAiPreRoundSelfChoice(
        FighterInstance owner, FighterInstance opponent, MatchState match, PersonaState state)
    {
        var options = owner.Definition.EvolutionCards;
        if (options.Count == 0) return null;

        int ownerDisabled = owner.LocationStates.Count(kv => kv.Value.State == Models.Fighter.DamageState.Disabled);
        if (ownerDisabled >= 1)
            return options.OrderByDescending(e => e.BaseDefense * 2 + e.BasePower).First().Id;
        return options.OrderByDescending(e => e.BasePower + e.BaseDefense + Math.Max(0, e.BaseSpeed)).First().Id;
    }

    // ---- Apply chosen evolution at resolution start ----

    public override void OnRoundResolutionStart(
        RoundState round,
        MatchState match,
        FighterInstance ownerFighter,
        FighterInstance opponent,
        PersonaState state)
    {
        if (!state.CustomData.TryGetValue(ChosenKey, out var raw)) return;
        string cardId = (string)raw;
        state.CustomData.Remove(ChosenKey);

        var evo = ownerFighter.Definition.EvolutionCards.FirstOrDefault(e => e.Id == cardId);
        if (evo == null) return;

        ownerFighter.RoundPowerModifier   += evo.BasePower;
        ownerFighter.RoundDefenseModifier += evo.BaseDefense;
        ownerFighter.RoundSpeedModifier   += evo.BaseSpeed;

        string spdStr = evo.BaseSpeed >= 0 ? $"+{evo.BaseSpeed}" : $"{evo.BaseSpeed}";
        round.Log.Add(
            $"[{ownerFighter.DisplayName}] Evolution '{evo.Name}' active: " +
            $"Pwr+{evo.BasePower} Def+{evo.BaseDefense} Spd{spdStr}");
    }

    // ---- Persona HUD ----

    public override List<string> GetHudDisplayInfo(PersonaState state)
    {
        if (state.CustomData.TryGetValue(ChosenKey, out var raw) && raw is string id && id.Length > 0)
            return new() { $"Evo queued: {id}" };
        return new() { "Evo: choose before selection" };
    }

    // ---- Mandatory overrides (no special logic needed) ----

    public override CardPair ModifyCardSelection(CardPair selectedPair, FighterInstance fighter, PersonaState state)
        => selectedPair;

    public override CardPair? GetPersonalizedAiDecision(
        FighterInstance ai, FighterInstance opponent, HexBoard board, PersonaState state)
        => null;
}
