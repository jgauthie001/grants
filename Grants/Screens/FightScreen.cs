using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using Grants.Engine;
using Grants.Fighters.Grants;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;
using Grants.UI;
using MatchType = Grants.Models.Match.MatchType;

namespace Grants.Screens;

/// <summary>
/// Main fight screen. Handles card selection UI, round resolution display,
/// hex board rendering, and damage state visualization.
/// </summary>
public partial class FightScreen : GameScreen
{
    private SpriteFont _font = null!;
    private SpriteFont _smallFont = null!;
    private Texture2D _pixel = null!;

    private MatchState _match = null!;
    private string _matchType = "pve";

    // Card selection state - two step: select generic first, then unique
    private bool _playerCommitted = false;
    
    // Two-step selection state
    private GenericCard? _selectedGeneric = null;
    private List<GenericCard> _validGenerics = new();
    private int _genericSelectionIndex = 0;
    private List<CardBase> _validUniques = new();  // Unique or Special cards
    private int _uniqueSelectionIndex = 0;

    // Local PvP
    private bool _isLocalPvP = false;
    private enum LocalPvpPhase { NotApplicable, P1Selecting, PassToP2, P2Selecting }
    private LocalPvpPhase _pvpPhase = LocalPvpPhase.NotApplicable;
    private GenericCard? _p2SelectedGeneric = null;
    private List<GenericCard> _p2ValidGenerics = new();
    private int _p2GenericIndex = 0;
    private List<CardBase> _p2ValidUniques = new();
    private int _p2UniqueIndex = 0;

    // Round log for display
    private List<string> _roundLog = new();
    // Step-wise resolution display
    private List<List<string>> _resolutionSteps = new();
    private int _stepIndex = 0;
    private bool _resolutionFullyDisplayed = false;
    private bool _needsSecondHalf = false;
    private int _firstHalfLogCount = 0;
    // Pre-round effect log (stage effects applied before card selection)
    private List<string> _preRoundLog = new();
    // Resign confirmation state
    private bool _resignPending = false;
    private KeyboardState _prevKeys;
    private MouseState _prevMouse;

    // Movement selection state
    private bool _awaitingMovement = false;
    private List<Models.Board.HexCoord> _validMoveHexes = new();
    private int _moveSelectionIndex = 0; // 0 = stay put (only when _moveForcedMin == 0); 1+ = index into _validMoveHexes
    private int _moveForcedMin = 0;      // > 0 means player cannot stay put; index starts at 1

    // Tooltip tracking
    private CardBase? _hoveredCard = null;
    private float _hoverTime = 0f;
    private const float HoverDelay = 0.5f;  // Delay before tooltip appears

    private FightLayout Layout =>
        new FightLayout(Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Height);

    // Stored so [R] Replay can restart the same match
    private object? _replayData;

    public override void OnEnter(object? data = null)
    {
        _font = Game.DefaultFont;
        _smallFont = Game.SmallFont;
        _pixel = Game.Pixel;
        _prevKeys = Keyboard.GetState();

        FighterDefinition fighterDefA;
        FighterDefinition fighterDefB;
        Models.Stage.StageModifier selectedStage = Models.Stage.ExhibitionStage.Instance;

        if (data is ValueTuple<FighterDefinition, FighterDefinition, string, Models.Stage.StageModifier> localPvpStage)
        {
            _isLocalPvP = true;
            (fighterDefA, fighterDefB, _matchType, var stageA) = localPvpStage;
            selectedStage = stageA;
        }
        else if (data is ValueTuple<FighterDefinition, FighterDefinition, string> localPvp)
        {
            _isLocalPvP = true;
            (fighterDefA, fighterDefB, _matchType) = localPvp;
        }
        else if (data is ValueTuple<FighterDefinition, string, Models.Stage.StageModifier> pveStage)
        {
            _isLocalPvP = false;
            (fighterDefA, _matchType, selectedStage) = pveStage;
            fighterDefB = GrantsFighter.CreateDefinition();
        }
        else
        {
            _isLocalPvP = false;
            var (fighterDef, matchType) = ((FighterDefinition, string))data!;
            fighterDefA = fighterDef;
            fighterDefB = GrantsFighter.CreateDefinition();
            _matchType = matchType;
        }

        // Create fighter instances
        var playerFighter = new FighterInstance(fighterDefA, _isLocalPvP ? "Player 1" : "Player");
        var p2Fighter     = new FighterInstance(fighterDefB, _isLocalPvP ? "Player 2" : "CPU Grants");

        // Apply upgrades from progress
        // TODO: upgrade system needs rework â€” disabled for now
        // var progressA = Game.PlayerProfile.GetOrCreateProgress(fighterDefA.Id);
        // var treeA = GrantsUpgradeTree.Create();
        // UpgradeEngine.ApplyProgressToInstance(playerFighter, progressA, treeA);

        if (!_isLocalPvP)
        {
            // var progressB = Game.PlayerProfile.GetOrCreateProgress(fighterDefB.Id);
            // var treeB = GrantsUpgradeTree.Create();
            // UpgradeEngine.ApplyProgressToInstance(p2Fighter, progressB, treeB);
        }

        _match = new MatchState
        {
            MatchType = _matchType switch
            {
                "pve"        => MatchType.PvE,
                "pvp_casual" => MatchType.PvpCasual,
                "pvp_ranked" => MatchType.PvpRanked,
                "pvp_local"  => MatchType.PvpCasual,
                _            => MatchType.PvE,
            },
            Stage = selectedStage,
            FighterA = playerFighter,
            FighterB = p2Fighter,
            FighterAIsHuman = true,
            FighterBIsHuman = _isLocalPvP,
        };

        _match.StageState = _match.Stage.CreateRuntimeState();
        _replayData = data;

        // Starting positions
        _match.FighterA.HexQ = Models.Board.HexBoard.FighterAStart.Q;
        _match.FighterA.HexR = Models.Board.HexBoard.FighterAStart.R;
        _match.FighterB.HexQ = Models.Board.HexBoard.FighterBStart.Q;
        _match.FighterB.HexR = Models.Board.HexBoard.FighterBStart.R;

        StartNewRound();
    }

    private void LoadAvailablePairs()
    {
        // Tick cooldowns now (start of selection phase) so a BaseCooldown=1 card
        // is unavailable for one full turn after being played.
        _match.FighterA.TickCooldowns();
        _match.FighterB.TickCooldowns();

        // Reset P1 two-step selection
        _selectedGeneric = null;
        var available = _match.FighterA.GetAvailableGenerics();
        _validGenerics = available;
        _genericSelectionIndex = 0;
        _uniqueSelectionIndex = 0;
        _validUniques.Clear();
        _playerCommitted = false;

        // Reset P2 selection state for local PvP
        if (_isLocalPvP)
        {
            _p2SelectedGeneric = null;
            _p2ValidGenerics = _match.FighterB.GetAvailableGenerics();
            _p2GenericIndex = 0;
            _p2UniqueIndex = 0;
            _p2ValidUniques.Clear();
            _pvpPhase = LocalPvpPhase.P1Selecting;
        }
        else
        {
            _pvpPhase = LocalPvpPhase.NotApplicable;
        }
    }

}
