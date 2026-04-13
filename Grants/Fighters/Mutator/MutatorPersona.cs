using Grants.Engine;
using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;

namespace Grants.Fighters.Mutator;

/// <summary>
/// The Mutator persona. At the start of each round (before card selection),
/// the Mutator may pick ONE unique card that is currently on cooldown and fold
/// it into their action as a "mutation". The chosen card's power, defense, and
/// speed are added as round modifiers — effectively making the Mutator a three-
/// card fighter (Generic + Unique + Mutation from cooldown).
///
/// Because the mutation provides a bonus, each of the Mutator's unique cards
/// is individually weaker than standard fighters' cards. The strategic depth
/// comes from choosing which cooling-down card to recycle each round.
///
/// A card on cooldown is not consumed or extended by being picked as a mutation —
/// the cooldown still ticks down normally.
/// </summary>
public class MutatorPersona : FighterPersona
{
    public static readonly MutatorPersona Instance = new()
    {
        PersonaId = "mutator",
        Name      = "Mutator",
        Description = "Folds a cooling card into the current action as a mutation. Adaptable but individually less powerful.",
    };

    private const string ChosenKey = "mutator_chosen_card";

    private MutatorPersona() { }

    public override PersonaState CreateRuntimeState() => new PersonaState();

    // --- Pre-round self-choice (generic protocol) ---

    public override PersonaChoiceRequest? GetPreRoundSelfChoice(
        FighterInstance owner, FighterInstance opponent, MatchState match, PersonaState state)
    {
        var options = owner.Definition.UniqueCards
            .Where(u => owner.GetCooldown(u.Id) > 0)
            .Select(u =>
            {
                int cd = owner.GetCooldown(u.Id);
                string spdStr = u.BaseSpeed >= 0 ? $"+{u.BaseSpeed}" : $"{u.BaseSpeed}";
                return new PersonaChoiceOption(
                    u.Id,
                    $"{u.Name}  Pwr:{u.BasePower} Def:{u.BaseDefense} Spd:{spdStr}  [CD:{cd}]");
            })
            .ToList();

        if (options.Count == 0) return null;

        return new PersonaChoiceRequest
        {
            Prompt  = "Pick one card from cooldown to fold into this round:",
            Options = options,
            CanSkip = true,
            HeaderTint = (180, 120, 220),
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
        var options = owner.Definition.UniqueCards
            .Where(u => owner.GetCooldown(u.Id) > 0)
            .ToList();
        if (options.Count == 0) return null;
        return options
            .OrderByDescending(u => owner.GetCardPower(u) + owner.GetCardDefense(u))
            .First().Id;
    }

    // --- Round start: apply chosen mutation ---

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

        var card = ownerFighter.Definition.UniqueCards.FirstOrDefault(u => u.Id == cardId);
        if (card == null) return;

        int p = ownerFighter.GetCardPower(card);
        int d = ownerFighter.GetCardDefense(card);
        int s = ownerFighter.GetCardSpeed(card);

        ownerFighter.RoundPowerModifier   += p;
        ownerFighter.RoundDefenseModifier += d;
        ownerFighter.RoundSpeedModifier   += s;

        string pStr = p != 0 ? $"+{p}P" : "";
        string dStr = d != 0 ? $"+{d}D" : "";
        string sStr = s != 0 ? $"+{s}Spd" : "";
        string bonus = string.Join(" ", new[] { pStr, dStr, sStr }.Where(x => x.Length > 0));
        round.Log.Add($"  [{ownerFighter.DisplayName}] Mutation: {card.Name} ({bonus})");
    }

    // --- HUD ---

    public override List<string> GetHudDisplayInfo(PersonaState state)
    {
        if (state.CustomData.TryGetValue(ChosenKey, out var raw))
            return new() { $"Mutate: {raw}" };
        return new();
    }

    // --- Standard pass-throughs ---

    public override CardPair ModifyCardSelection(CardPair selectedPair, FighterInstance fighter, PersonaState state)
        => selectedPair;

    public override CardPair? GetPersonalizedAiDecision(
        FighterInstance ai, FighterInstance opponent, HexBoard board, PersonaState state)
        => null;
}
