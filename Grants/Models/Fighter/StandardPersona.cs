using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Match;

namespace Grants.Models.Fighter;

/// <summary>
/// Default persona for fighters with no special mechanics.
/// Provides the baseline behavior — all hook methods are no-ops.
/// Other personas inherit or delegate to StandardPersona for common behavior.
/// </summary>
public class StandardPersona : FighterPersona
{
    public static readonly StandardPersona Instance = new()
    {
        PersonaId = "standard",
        Name = "Standard Combatant",
        Description = "No special abilities. Balanced, straightforward fighter."
    };

    private StandardPersona() { }

    public override PersonaState CreateRuntimeState()
    {
        return new PersonaState();
    }

    public override CardPair ModifyCardSelection(
        CardPair selectedPair,
        FighterInstance fighter,
        PersonaState state)
    {
        // No modification — pass through
        return selectedPair;
    }

    public override CardPair? GetPersonalizedAiDecision(
        FighterInstance ai,
        FighterInstance opponent,
        HexBoard board,
        PersonaState state)
    {
        // Return null to use default AiEngine logic
        return null;
    }

    public override void OnRoundResolutionStart(
        RoundState round,
        MatchState match,
        FighterInstance ownerFighter,
        FighterInstance opponent,
        PersonaState state)
    {
        // No special round logic
    }
}
