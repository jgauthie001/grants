using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Board;

namespace Grants.Models.Match;

public enum MatchType { PvE, PvpCasual, PvpRanked }

public enum MatchPhase
{
    CardSelection,      // Both fighters choosing their pair
    ResolutionMovement, // Applying movement in speed order
    ResolutionAttack,   // Applying attacks in speed order
    RoundResult,        // Displaying round outcome before next round
    MatchOver,          // One fighter KO'd
}

public enum RoundOutcome
{
    Pending,
    FighterAWins,       // Fighter A landed, B missed or was faster and countered
    FighterBWins,
    BothHit,            // Simultaneous — both landed
    BothMissed,         // Out of range or both defended
    FighterACountered,  // B's parry triggered
    FighterBCountered,
}

/// <summary>
/// All state for one round of combat, after both cards are committed.
/// </summary>
public class RoundState
{
    public int RoundNumber { get; init; }
    public CardPair PairA { get; init; } = null!;
    public CardPair PairB { get; init; } = null!;

    // Computed speeds
    public int SpeedA { get; init; }
    public int SpeedB { get; init; }
    public bool FighterAFaster => SpeedA > SpeedB;
    public bool FighterBFaster => SpeedB > SpeedA;
    public bool SpeedTie => SpeedA == SpeedB;

    // Post-resolution
    public RoundOutcome Outcome { get; set; } = RoundOutcome.Pending;

    // Damage applied this round (location → damage steps)
    public Dictionary<BodyLocation, int> DamageToA { get; set; } = new();
    public Dictionary<BodyLocation, int> DamageToB { get; set; } = new();

    // Narrative log lines for display
    public List<string> Log { get; set; } = new();

    // Status effects applied this round
    public List<string> StatusEffectsOnA { get; set; } = new();
    public List<string> StatusEffectsOnB { get; set; } = new();

    // Did each fighter miss due to movement?
    public bool FighterAMissed { get; set; } = false;
    public bool FighterBMissed { get; set; } = false;
}

/// <summary>
/// Top-level match state. Persists across all rounds until KO.
/// </summary>
public class MatchState
{
    public MatchType MatchType { get; init; }
    public MatchPhase Phase { get; set; } = MatchPhase.CardSelection;

    public FighterInstance FighterA { get; init; } = null!;
    public FighterInstance FighterB { get; init; } = null!;

    public HexBoard Board { get; init; } = new();

    public int CurrentRound { get; set; } = 1;
    public List<RoundState> History { get; private set; } = new();

    // Card selections for current round (set during CardSelection phase)
    public CardPair? SelectedPairA { get; set; }
    public CardPair? SelectedPairB { get; set; }

    // Whether each fighter is human or AI-controlled
    public bool FighterAIsHuman { get; init; } = true;
    public bool FighterBIsHuman { get; init; } = false;

    // Match result
    public FighterInstance? Winner { get; set; }
    public FighterInstance? Loser { get; set; }
    public bool IsOver => Phase == MatchPhase.MatchOver;

    public RoundState? CurrentRoundState { get; set; }

    public FighterInstance Opponent(FighterInstance fighter) =>
        fighter == FighterA ? FighterB : FighterA;

    public bool IsFighterA(FighterInstance fighter) => fighter == FighterA;
}
