using Grants.Engine;
using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;

namespace Grants.Fighters.Chivalrous;

/// <summary>
/// The Chivalrous persona. Character-specific mechanics:
///
/// Chivalry Tokens:
///   - Every time Chivalrous lands a hit, the opponent gains 1 Chivalry token.
///   - Tokens are stored on the OPPONENT's PersonaState under "chivalry_tokens".
///   - No cap — tokens accumulate across rounds.
///
/// Opponent token spending (before card selection each round):
///   - If the opponent holds tokens, they are offered a choice:
///     "Spend all N tokens for +N Power / +N Speed this round?"
///   - Accepting drains all tokens and applies the buffs for that round.
///   - The opponent can decline and save tokens for a bigger buff later.
///   - AI: always accepts (free stats).
///
/// Card synergies (handled in AttackEngine):
///   ChivalryBonus — +N damage steps if defender holds >=1 chivalry token
///   Pull          — pull opponent 1 hex closer on hit
///   DistanceGuard — +2 defense if current distance >= threshold
/// </summary>
public class ChivalrousPersona : FighterPersona
{
    public static readonly ChivalrousPersona Instance = new()
    {
        PersonaId = "chivalrous",
        Name     = "The Chivalrous",
        Description = "Awards opponents chivalry tokens on hit; they can spend tokens for speed and power.",
    };

    private ChivalrousPersona() { }

    private const string KeyTokens = "chivalry_tokens"; // stored on OPPONENT's PersonaState

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public override PersonaState CreateRuntimeState() => new PersonaState();

    public override CardPair ModifyCardSelection(CardPair selectedPair, FighterInstance fighter, PersonaState state)
        => selectedPair;

    public override CardPair? GetPersonalizedAiDecision(FighterInstance ai, FighterInstance opponent, HexBoard board, PersonaState state)
        => null;

    public override void OnRoundResolutionStart(RoundState round, MatchState match, FighterInstance ownerFighter, FighterInstance opponent, PersonaState state) { }

    // ─── Hit hook ─────────────────────────────────────────────────────────────

    public override void OnLandedHit(
        FighterInstance attacker,
        FighterInstance defender,
        CardPair attackerPair,
        AttackEngine.AttackResult result,
        RoundState round,
        MatchState match,
        PersonaState state)
    {
        // Only give token when THIS persona's owner is the attacker
        if (!object.ReferenceEquals(state, attacker.PersonaState)) return;

        int current = defender.PersonaState.Counters.GetValueOrDefault(KeyTokens, 0);
        defender.PersonaState.Counters[KeyTokens] = current + 1;
        round.Log.Add($"  [Chivalrous] {defender.DisplayName} receives a chivalry token. ({current + 1} total)");
    }

    // ─── Opponent choice hooks ────────────────────────────────────────────────

    public override bool RequiresOpponentRoundStartChoice(
        FighterInstance owner,
        FighterInstance opponent,
        MatchState match,
        PersonaState state)
        => opponent.PersonaState.Counters.GetValueOrDefault(KeyTokens, 0) > 0;

    public override string GetOpponentChoicePrompt(
        FighterInstance owner,
        FighterInstance opponent,
        PersonaState state)
    {
        int tokens = opponent.PersonaState.Counters.GetValueOrDefault(KeyTokens, 0);
        return $"You hold {tokens} chivalry token(s). Spend all for +{tokens} Spd / +{tokens} Pwr this round?";
    }

    public override bool ResolveAiOpponentChoice(
        FighterInstance owner,
        FighterInstance opponent,
        MatchState match,
        PersonaState state)
        => true; // Always spend — free stats

    public override void OnOpponentChoice(
        FighterInstance owner,
        FighterInstance opponent,
        bool accepted,
        MatchState match,
        PersonaState state)
    {
        if (!accepted) return;

        int tokens = opponent.PersonaState.Counters.GetValueOrDefault(KeyTokens, 0);
        if (tokens <= 0) return;

        opponent.PersonaState.Counters[KeyTokens] = 0;
        opponent.RoundPowerModifier += tokens;
        opponent.RoundSpeedModifier += tokens;
    }

    // ─── HUD ──────────────────────────────────────────────────────────────────

    public override List<string> GetHudDisplayInfo(PersonaState state)
        => new() { "CHIVALROUS" };
}
