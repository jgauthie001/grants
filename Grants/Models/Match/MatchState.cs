using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Board;
using Grants.Models.Stage;
using Grants.Engine;

namespace Grants.Models.Match;

public enum MatchType { PvE, PvpCasual, PvpRanked }

public enum MatchPhase
{
    CardSelection,      // Both fighters choosing their pair
    CardReveal,         // Both cards committed — show both pairs before resolution begins
    RoundMidpoint,      // Pause after first fighter acts, before second
    RoundResult,        // Displaying round outcome before next round
    StageChoiceA,       // Stage-specific choice prompt for Fighter A
    StageChoiceB,       // Stage-specific choice prompt for Fighter B
    PersonaChoiceA,     // Fighter A is offered a choice by Fighter B's persona
    PersonaChoiceB,     // Fighter B is offered a choice by Fighter A's persona
    PersonaSelfChoiceA, // Fighter A is offered a self-choice by their OWN persona
    PersonaSelfChoiceB, // Fighter B is offered a self-choice by their OWN persona
    PreRoundSelfChoiceA, // Persona: Fighter A makes a pre-round list-selection (generic protocol)
    PreRoundSelfChoiceB, // Persona: Fighter B makes a pre-round list-selection (generic protocol)
    MatchOver,          // One fighter KO'd
}

public enum RoundOutcome
{
    Pending,
    FighterAWins,       // Fighter A landed, B missed
    FighterBWins,
    BothHit,            // Simultaneous — both landed
    BothMissed,         // Out of range or both defended
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

    // Did each fighter miss due to movement?
    public bool FighterAMissed { get; set; } = false;
    public bool FighterBMissed { get; set; } = false;

    // Most recent location hit on each fighter (used by stage hooks for damage reduction)
    public BodyLocation? LastHitOnA { get; set; } = null;
    public BodyLocation? LastHitOnB { get; set; } = null;

    // How many log lines belong to the first fighter's action (for mid-round pause display)
    public int FirstHalfLogCount { get; set; } = 0;

    /// <summary>
    /// Cached priority player attack result set in ResolveFirstHalf.
    /// Used by ResolveSecondHalf to apply the priority Final phase.
    /// </summary>
    internal AttackEngine.AttackResult? PriorityAttackResult { get; set; }

    /// <summary>
    /// Cached second-player attack result set in ResolveFirstHalf/ResolveSecondHalf.
    /// Used by ResolveSecondHalf to apply the second fighter's Final phase post-move.
    /// </summary>
    internal AttackEngine.AttackResult? SecondAttackResult { get; set; }
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

    // Stage modifier for environmental effects
    public StageModifier Stage { get; init; } = StandardStage.Instance;
    public StageState StageState { get; set; } = null!;

    // Card selections for current round (set during CardSelection phase)
    public CardPair? SelectedPairA { get; set; }
    public CardPair? SelectedPairB { get; set; }

    // Player's explicit movement destination (null = auto-move toward opponent)
    public HexCoord? ChosenMoveA { get; set; }

    // Whether each fighter is human or AI-controlled
    public bool FighterAIsHuman { get; init; } = true;
    public bool FighterBIsHuman { get; init; } = false;

    /// <summary>
    /// When false, no upgrade effects are applied to either fighter.
    /// Defaults true for PvE/Casual. Set false for Ranked or specific game modes.
    /// </summary>
    public bool UpgradesEnabled { get; init; } = true;

    // Match result
    public FighterInstance? Winner { get; set; }
    public FighterInstance? Loser { get; set; }
    public bool IsDraw { get; set; } = false;
    public bool IsOver => Phase == MatchPhase.MatchOver;

    // Stalemate tracking: incremented each round nobody takes damage
    public int ConsecutiveNoDamageRounds { get; set; } = 0;

    public RoundState? CurrentRoundState { get; set; }

    public FighterInstance Opponent(FighterInstance fighter) =>
        fighter == FighterA ? FighterB : FighterA;

    public bool IsFighterA(FighterInstance fighter) => fighter == FighterA;
}
