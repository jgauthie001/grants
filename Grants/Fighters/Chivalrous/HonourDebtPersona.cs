using Grants.Engine;
using Grants.Models.Board;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;

namespace Grants.Fighters.Chivalrous;

/// <summary>
/// The Honour Debt persona — a ruthless variant of The Chivalrous.
///
/// Honour Debt Tokens:
///   - Every time Honour Debt lands a hit, the opponent gains 1 Honour Debt token.
///   - Tokens are stored on the OPPONENT's PersonaState under "honour_debt_tokens".
///   - No cap — tokens accumulate across rounds.
///
/// Owner passive speed bonus (applied at round resolution start):
///   - Honour Debt gains +1 Speed this round for each token currently on the opponent.
///   - This is automatic — no choice required. The more tokens the opponent carries,
///     the faster Honour Debt moves.
///
/// Opponent token spending (before card selection each round):
///   - If the opponent holds tokens, they are offered a choice:
///     "Spend all N tokens for +2N Power this round? (no speed bonus)"
///   - Tokens are worth 2 Power each instead of 1 Power + 1 Speed.
///   - The opponent can decline and save tokens for a bigger power burst later.
///   - AI: always accepts (free stats).
///
/// The design asymmetry: opponent gets more raw power per token, but
/// Honour Debt keeps gaining speed as long as the opponent holds tokens.
/// Spending tokens early gives less power but stops Honour Debt's speed advantage.
/// Saving tokens gives a bigger power spike but Honour Debt accelerates.
/// </summary>
public class HonourDebtPersona : FighterPersona
{
    public static readonly HonourDebtPersona Instance = new()
    {
        PersonaId   = "honour_debt",
        Name        = "Honour Debt",
        Description = "Burdens opponents with debt tokens, gaining speed while they carry them. Tokens pay out double power.",
    };

    private HonourDebtPersona() { }

    private const string KeyTokens = "honour_debt_tokens"; // stored on OPPONENT's PersonaState

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public override PersonaState CreateRuntimeState() => new PersonaState();

    public override CardPair ModifyCardSelection(CardPair selectedPair, FighterInstance fighter, PersonaState state)
        => selectedPair;

    public override CardPair? GetPersonalizedAiDecision(FighterInstance ai, FighterInstance opponent, HexBoard board, PersonaState state)
        => null;

    // ─── Passive speed bonus ──────────────────────────────────────────────────

    public override void OnRoundResolutionStart(
        RoundState round,
        MatchState match,
        FighterInstance ownerFighter,
        FighterInstance opponent,
        PersonaState state)
    {
        // state belongs to ownerFighter; tokens are on the OPPONENT's PersonaState
        int tokens = opponent.PersonaState.Counters.GetValueOrDefault(KeyTokens, 0);
        if (tokens <= 0) return;

        ownerFighter.RoundSpeedModifier += tokens;
        round.Log.Add($"  [Honour Debt] {ownerFighter.DisplayName} gains +{tokens} Speed from {tokens} debt token(s) on {opponent.DisplayName}.");
    }

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
        round.Log.Add($"  [Honour Debt] {defender.DisplayName} receives a debt token. ({current + 1} total)");
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
        return $"You carry {tokens} debt token(s). Spend all for +{tokens * 2} Pwr this round? (no speed bonus)";
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
        opponent.RoundPowerModifier += tokens * 2; // 2 Power per token, no speed
    }

    // ─── HUD ──────────────────────────────────────────────────────────────────

    public override List<string> GetHudDisplayInfo(PersonaState state)
        => new() { "HONOUR DEBT" };
}
