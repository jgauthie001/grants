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

public partial class FightScreen
{
    public override void Update(GameTime gameTime)
    {
        try
        {
            var keys = Keyboard.GetState();
            var mouse = Mouse.GetState();

            // Update hover detection
            UpdateCardHover(mouse, gameTime);

            // Resign confirmation overlay takes full priority
            if (_resignPending)
            {
                if (IsPressed(keys, _prevKeys, Keys.Y))
                    Resign();
                else if (IsPressed(keys, _prevKeys, Keys.N) || IsPressed(keys, _prevKeys, Keys.Escape))
                    _resignPending = false;
                _prevKeys = keys;
                _prevMouse = mouse;
                return;
            }

            if (_match.Phase == MatchPhase.CardSelection && !_playerCommitted)
            {
                if (_selectedGeneric == null)
                {
                    // Step 1: Select generic card
                    if (IsPressed(keys, _prevKeys, Keys.Left) || IsPressed(keys, _prevKeys, Keys.A))
                        _genericSelectionIndex = (_genericSelectionIndex - 1 + _validGenerics.Count) % _validGenerics.Count;

                    if (IsPressed(keys, _prevKeys, Keys.Right) || IsPressed(keys, _prevKeys, Keys.D))
                        _genericSelectionIndex = (_genericSelectionIndex + 1) % _validGenerics.Count;

                    if (IsPressed(keys, _prevKeys, Keys.Enter))
                    {
                        if (_validGenerics.Count > 0)
                        {
                            _selectedGeneric = _validGenerics[_genericSelectionIndex];
                            // Populate valid uniques for this generic
                            var uniques = _match.FighterA.GetAvailableUniques()
                                .Where(u => _match.FighterA.CanPair(_selectedGeneric, u))
                                .Cast<CardBase>()
                                .ToList();
                            // Add available standalone specials
                            uniques.AddRange(_match.FighterA.GetAvailableSpecials()
                                .Where(s => s.Standalone)
                                .Cast<CardBase>());
                            _validUniques = uniques;
                            _uniqueSelectionIndex = 0;
                        }
                    }
                }
                else
                {
                    // Step 2: Select unique/special card
                    if (IsPressed(keys, _prevKeys, Keys.Left) || IsPressed(keys, _prevKeys, Keys.A))
                        _uniqueSelectionIndex = (_uniqueSelectionIndex - 1 + _validUniques.Count) % _validUniques.Count;

                    if (IsPressed(keys, _prevKeys, Keys.Right) || IsPressed(keys, _prevKeys, Keys.D))
                        _uniqueSelectionIndex = (_uniqueSelectionIndex + 1) % _validUniques.Count;

                    if (IsPressed(keys, _prevKeys, Keys.Enter))
                    {
                        CommitPlayerChoice();
                    }
                    
                    if (IsPressed(keys, _prevKeys, Keys.Back))
                    {
                        _selectedGeneric = null;
                        _validUniques.Clear();
                    }
                }
            }
            else if (_match.Phase == MatchPhase.CardSelection && _pvpPhase == LocalPvpPhase.PassToP2)
            {
                // Show "pass controller" screen; P2 confirms when ready
                if (IsPressed(keys, _prevKeys, Keys.Enter) || IsPressed(keys, _prevKeys, Keys.Space))
                    LoadP2AvailablePairs();
            }
            else if (_match.Phase == MatchPhase.CardSelection && _pvpPhase == LocalPvpPhase.P2Selecting)
            {
                if (_p2SelectedGeneric == null)
                {
                    // P2 Step 1: Select generic card
                    if (IsPressed(keys, _prevKeys, Keys.Left) || IsPressed(keys, _prevKeys, Keys.A))
                        _p2GenericIndex = (_p2GenericIndex - 1 + _p2ValidGenerics.Count) % _p2ValidGenerics.Count;

                    if (IsPressed(keys, _prevKeys, Keys.Right) || IsPressed(keys, _prevKeys, Keys.D))
                        _p2GenericIndex = (_p2GenericIndex + 1) % _p2ValidGenerics.Count;

                    if (IsPressed(keys, _prevKeys, Keys.Enter))
                    {
                        if (_p2ValidGenerics.Count > 0)
                        {
                            _p2SelectedGeneric = _p2ValidGenerics[_p2GenericIndex];
                            var uniques = _match.FighterB.GetAvailableUniques()
                                .Where(u => _match.FighterB.CanPair(_p2SelectedGeneric, u))
                                .Cast<CardBase>()
                                .ToList();
                            uniques.AddRange(_match.FighterB.GetAvailableSpecials()
                                .Where(s => s.Standalone)
                                .Cast<CardBase>());
                            _p2ValidUniques = uniques;
                            _p2UniqueIndex = 0;
                        }
                    }
                }
                else
                {
                    // P2 Step 2: Select unique/special card
                    if (IsPressed(keys, _prevKeys, Keys.Left) || IsPressed(keys, _prevKeys, Keys.A))
                        _p2UniqueIndex = (_p2UniqueIndex - 1 + _p2ValidUniques.Count) % _p2ValidUniques.Count;

                    if (IsPressed(keys, _prevKeys, Keys.Right) || IsPressed(keys, _prevKeys, Keys.D))
                        _p2UniqueIndex = (_p2UniqueIndex + 1) % _p2ValidUniques.Count;

                    if (IsPressed(keys, _prevKeys, Keys.Enter))
                        CommitP2Choice();

                    if (IsPressed(keys, _prevKeys, Keys.Back))
                    {
                        _p2SelectedGeneric = null;
                        _p2ValidUniques.Clear();
                    }
                }
            }
            else if (_match.Phase == MatchPhase.CardSelection && _awaitingMovement)
            {
                // Movement destination selection
                // When _moveForcedMin > 0: index 1..N only (no stay-put); otherwise 0..N (0 = stay put)
                int minIdx = _moveForcedMin > 0 ? 1 : 0;
                int totalOpts = _validMoveHexes.Count + 1; // slot 0 always exists for stay-put math; unused if forced

                if (IsPressed(keys, _prevKeys, Keys.Right) || IsPressed(keys, _prevKeys, Keys.D) || IsPressed(keys, _prevKeys, Keys.Tab))
                {
                    _moveSelectionIndex++;
                    if (_moveSelectionIndex >= totalOpts) _moveSelectionIndex = minIdx;
                }
                if (IsPressed(keys, _prevKeys, Keys.Left) || IsPressed(keys, _prevKeys, Keys.A))
                {
                    _moveSelectionIndex--;
                    if (_moveSelectionIndex < minIdx) _moveSelectionIndex = totalOpts - 1;
                }

                if (IsPressed(keys, _prevKeys, Keys.Enter) || IsPressed(keys, _prevKeys, Keys.Space))
                {
                    _match.ChosenMoveA = _moveSelectionIndex == 0 ? null : _validMoveHexes[_moveSelectionIndex - 1];
                    _awaitingMovement = false;
                    ShowCardReveal();
                }

                if (IsPressed(keys, _prevKeys, Keys.Escape))
                {
                    // Forced movement: can't cancel — use first valid hex
                    _match.ChosenMoveA = _moveForcedMin > 0 && _validMoveHexes.Count > 0
                        ? _validMoveHexes[0]
                        : null;
                    _awaitingMovement = false;
                    ShowCardReveal();
                }

                // Click on a valid hex to move there
                if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                {
                    var L = Layout;
                    for (int i = 0; i < _validMoveHexes.Count; i++)
                    {
                        var (hx, hy) = Models.Board.HexBoard.HexToPixel(_validMoveHexes[i], L.HexSize, L.BoardCenterX, L.BoardCenterY);
                        if (Vector2.Distance(new Vector2(mouse.X, mouse.Y), new Vector2(hx, hy)) <= L.HexSize)
                        {
                            _match.ChosenMoveA = _validMoveHexes[i];
                            _awaitingMovement = false;
                            ShowCardReveal();
                            break;
                        }
                    }
                }
            }
            else if (_match.Phase == MatchPhase.CardReveal)
            {
                if (IsPressed(keys, _prevKeys, Keys.Enter) || IsPressed(keys, _prevKeys, Keys.Space))
                    ExecuteRound();
            }
            else if (_match.Phase == MatchPhase.RoundMidpoint ||
                     (_match.Phase == MatchPhase.RoundResult && !_resolutionFullyDisplayed))
            {
                if (IsPressed(keys, _prevKeys, Keys.Enter) || IsPressed(keys, _prevKeys, Keys.Space))
                    AdvanceResolutionStep();
            }
            else if (_match.Phase == MatchPhase.RoundResult && _resolutionFullyDisplayed)
            {
                if (IsPressed(keys, _prevKeys, Keys.Enter) || IsPressed(keys, _prevKeys, Keys.Space))
                    StartNewRound();
            }
            else if (_match.Phase == MatchPhase.StageChoiceA)
            {
                if (IsPressed(keys, _prevKeys, Keys.Y))
                {
                    _match.Stage.OnFighterChoice(_match.FighterA, true, _match, _match.StageState);
                    AdvanceStageChoices(skipA: true);
                }
                else if (IsPressed(keys, _prevKeys, Keys.N))
                {
                    _match.Stage.OnFighterChoice(_match.FighterA, false, _match, _match.StageState);
                    AdvanceStageChoices(skipA: true);
                }
            }
            else if (_match.Phase == MatchPhase.StageChoiceB)
            {
                if (IsPressed(keys, _prevKeys, Keys.Y))
                {
                    _match.Stage.OnFighterChoice(_match.FighterB, true, _match, _match.StageState);
                    EnterCardSelection();
                }
                else if (IsPressed(keys, _prevKeys, Keys.N))
                {
                    _match.Stage.OnFighterChoice(_match.FighterB, false, _match, _match.StageState);
                    EnterCardSelection();
                }
            }
            else if (_match.Phase == MatchPhase.PersonaChoiceA)
            {
                if (IsPressed(keys, _prevKeys, Keys.Y))
                {
                    _match.FighterB.Definition.Persona.OnOpponentChoice(
                        _match.FighterB, _match.FighterA, true, _match, _match.FighterB.PersonaState);
                    _preRoundLog.Add("You spend a Curse token (-1 Power / -1 Speed this round).");
                    AdvanceAfterFighterBPersonaChoice();
                }
                else if (IsPressed(keys, _prevKeys, Keys.N))
                {
                    _match.FighterB.Definition.Persona.OnOpponentChoice(
                        _match.FighterB, _match.FighterA, false, _match, _match.FighterB.PersonaState);
                    AdvanceAfterFighterBPersonaChoice();
                }
            }
            else if (_match.Phase == MatchPhase.PersonaChoiceB)
            {
                if (IsPressed(keys, _prevKeys, Keys.Y))
                {
                    _match.FighterA.Definition.Persona.OnOpponentChoice(
                        _match.FighterA, _match.FighterB, true, _match, _match.FighterA.PersonaState);
                    _preRoundLog.Add("You spend a Curse token (-1 Power / -1 Speed this round).");
                    EnterCardSelection();
                }
                else if (IsPressed(keys, _prevKeys, Keys.N))
                {
                    _match.FighterA.Definition.Persona.OnOpponentChoice(
                        _match.FighterA, _match.FighterB, false, _match, _match.FighterA.PersonaState);
                    EnterCardSelection();
                }
            }
            else if (_match.Phase == MatchPhase.PersonaSelfChoiceA)
            {
                if (IsPressed(keys, _prevKeys, Keys.Y))
                {
                    _match.FighterA.Definition.Persona.OnSelfChoice(
                        _match.FighterA, _match.FighterB, true, _match, _match.FighterA.PersonaState);
                    _preRoundLog.Add(_match.FighterA.Definition.Persona.GetSelfChoicePrompt(
                        _match.FighterA, _match.FighterB, _match.FighterA.PersonaState) is var p && p.Length > 0
                        ? $"{_match.FighterA.DisplayName} spends from pool."
                        : $"{_match.FighterA.DisplayName} activates self-choice.");
                    AdvancePersonaSelfChoiceB();
                }
                else if (IsPressed(keys, _prevKeys, Keys.N))
                {
                    _match.FighterA.Definition.Persona.OnSelfChoice(
                        _match.FighterA, _match.FighterB, false, _match, _match.FighterA.PersonaState);
                    AdvancePersonaSelfChoiceB();
                }
            }
            else if (_match.Phase == MatchPhase.PersonaSelfChoiceB)
            {
                if (IsPressed(keys, _prevKeys, Keys.Y))
                {
                    _match.FighterB.Definition.Persona.OnSelfChoice(
                        _match.FighterB, _match.FighterA, true, _match, _match.FighterB.PersonaState);
                    _preRoundLog.Add($"{_match.FighterB.DisplayName} spends from pool.");
                    AdvancePreRoundSelfChoiceA();
                }
                else if (IsPressed(keys, _prevKeys, Keys.N))
                {
                    _match.FighterB.Definition.Persona.OnSelfChoice(
                        _match.FighterB, _match.FighterA, false, _match, _match.FighterB.PersonaState);
                    AdvancePreRoundSelfChoiceA();
                }
            }
            else if (_match.Phase == MatchPhase.PreRoundSelfChoiceA || _match.Phase == MatchPhase.PreRoundSelfChoiceB)
            {
                bool isA = _match.Phase == MatchPhase.PreRoundSelfChoiceA;
                if (_preRoundChoice != null && _preRoundChoice.Options.Count > 0)
                {
                    int count = _preRoundChoice.Options.Count;
                    if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
                        _preRoundChoiceIndex = (_preRoundChoiceIndex - 1 + count) % count;
                    if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
                        _preRoundChoiceIndex = (_preRoundChoiceIndex + 1) % count;
                    if (IsPressed(keys, _prevKeys, Keys.Enter))
                    {
                        var owner = isA ? _match.FighterA : _match.FighterB;
                        var chosen = _preRoundChoice.Options[_preRoundChoiceIndex];
                        owner.Definition.Persona.OnPreRoundSelfChoiceSelected(owner, chosen.Id, _match, owner.PersonaState);
                        _preRoundLog.Add($"[{owner.DisplayName}] {chosen.Label}");
                        if (isA) AdvancePreRoundSelfChoiceB(); else EnterCardSelection();
                    }
                }
                if (_preRoundChoice?.CanSkip == true && IsPressed(keys, _prevKeys, Keys.Back))
                {
                    var owner = isA ? _match.FighterA : _match.FighterB;
                    owner.Definition.Persona.OnPreRoundSelfChoiceSelected(owner, null, _match, owner.PersonaState);
                    if (isA) AdvancePreRoundSelfChoiceB(); else EnterCardSelection();
                }
            }
            else if (_match.Phase == MatchPhase.MatchOver)
            {
                if (IsPressed(keys, _prevKeys, Keys.Enter) || IsPressed(keys, _prevKeys, Keys.Space))
                {
                    if (!_resolutionFullyDisplayed && _resolutionSteps.Count > 0)
                    {
                        _stepIndex = Math.Min(_stepIndex + 1, _resolutionSteps.Count - 1);
                        if (_stepIndex >= _resolutionSteps.Count - 1)
                            _resolutionFullyDisplayed = true;
                    }
                    else
                    {
                        HandleMatchEnd();
                    }
                }
                if (IsPressed(keys, _prevKeys, Keys.R) && _resolutionFullyDisplayed)
                    ReplayMatch();
            }

            if (IsPressed(keys, _prevKeys, Keys.Escape))
            {
                if (_match.Phase == MatchPhase.MatchOver)
                    HandleMatchEnd();
                else
                    _resignPending = true;
            }

            _prevKeys = keys;
            _prevMouse = mouse;
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game_debug.log");
            using (var writer = new StreamWriter(logPath, append: true))
            {
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] UPDATE ERROR: {ex.Message}");
                writer.WriteLine(ex.StackTrace);
                writer.Flush();
            }
            throw;
        }
    }

    private void CommitPlayerChoice()
    {
        if (_selectedGeneric == null || _validUniques.Count == 0) return;

        var unique = _validUniques[_uniqueSelectionIndex];
        var pair = new CardPair
        {
            Generic = _selectedGeneric,
            Unique = unique as UniqueCard,
            Special = unique as SpecialCard,
        };

        _match.SelectedPairA = pair;
        _playerCommitted = true;
        _selectedGeneric = null;
        _validUniques.Clear();

        if (_isLocalPvP)
        {
            // Don't resolve yet — wait for P2 to pick
            _pvpPhase = LocalPvpPhase.PassToP2;
            return;
        }

        // PvE / online PvP: AI picks immediately
        _match.SelectedPairB = AiEngine.SelectPair(_match.FighterB, _match.FighterA, _match.Board);
        StartMovementOrExecute();
    }

    private void LoadP2AvailablePairs()
    {
        _p2SelectedGeneric = null;
        _p2ValidGenerics = _match.FighterB.GetAvailableGenerics();
        _p2GenericIndex = 0;
        _p2UniqueIndex = 0;
        _p2ValidUniques.Clear();
        _pvpPhase = LocalPvpPhase.P2Selecting;
    }

    private void CommitP2Choice()
    {
        if (_p2SelectedGeneric == null || _p2ValidUniques.Count == 0) return;

        var unique = _p2ValidUniques[_p2UniqueIndex];
        var pair = new CardPair
        {
            Generic = _p2SelectedGeneric,
            Unique = unique as UniqueCard,
            Special = unique as SpecialCard,
        };

        _match.SelectedPairB = pair;
        _p2SelectedGeneric = null;
        _p2ValidUniques.Clear();
        _pvpPhase = LocalPvpPhase.NotApplicable;
        StartMovementOrExecute();
    }

    private void StartMovementOrExecute()
    {
        var pair = _match.SelectedPairA!;

        // Pre-attack movement is driven by the generic card only.
        int maxMovement = _match.FighterA.GetCardMovement(pair.Generic!);
        int minMovement = pair.EffectiveMinMovement;

        if (maxMovement > 0)
        {
            var pos = new Models.Board.HexCoord(_match.FighterA.HexQ, _match.FighterA.HexR);
            var oppPos = new Models.Board.HexCoord(_match.FighterB.HexQ, _match.FighterB.HexR);
            _match.Board.SetOccupied(oppPos, true);
            _validMoveHexes = Engine.MovementEngine.GetReachableHexes(_match.FighterA, pair, pos, oppPos, _match.Board);
            _match.Board.SetOccupied(oppPos, false);
            _moveForcedMin = minMovement;
            if (_moveForcedMin > 0 && _validMoveHexes.Count == 0)
            {
                _match.ChosenMoveA = null;
                ShowCardReveal();
                return;
            }
            _moveSelectionIndex = _moveForcedMin > 0 ? 1 : 0;
            _awaitingMovement = true;
        }
        else
        {
            _match.ChosenMoveA = null;
            ShowCardReveal();
        }
    }

    private void ShowCardReveal()
    {
        _match.Phase = MatchPhase.CardReveal;
    }

    private static bool IsPressed(KeyboardState cur, KeyboardState prev, Keys key) =>
        cur.IsKeyDown(key) && prev.IsKeyUp(key);

    private void UpdateCardHover(MouseState mouse, GameTime gameTime)
    {
        if (_match.Phase != MatchPhase.CardSelection || _playerCommitted)
        {
            _hoveredCard = null;
            _hoverTime = 0f;
            return;
        }

        CardBase? cardUnderMouse = null;

        // Check generic cards (if in step 1)
        if (_selectedGeneric == null)
        {
            const int panelX = 20, panelY = 200 + 24;
            const int lineHeight = 18;
            int cardY = panelY;

            for (int i = 0; i < _validGenerics.Count; i++)
            {
                var rect = new Rectangle(panelX, cardY + i * lineHeight, 400, lineHeight - 2);
                if (rect.Contains(mouse.Position))
                {
                    cardUnderMouse = _validGenerics[i];
                    break;
                }
            }
        }
        // Check unique/special cards (if in step 2)
        else if (_selectedGeneric != null && _validUniques.Count > 0)
        {
            const int panelX = 20, panelY = 200 + 24;
            const int lineHeight = 18;
            int cardY = panelY;

            for (int i = 0; i < _validUniques.Count; i++)
            {
                var rect = new Rectangle(panelX, cardY + i * lineHeight, 400, lineHeight - 2);
                if (rect.Contains(mouse.Position))
                {
                    cardUnderMouse = _validUniques[i];
                    break;
                }
            }
        }

        // Update hover state
        if (cardUnderMouse == _hoveredCard)
        {
            // Still hovering same card
            _hoverTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
        else
        {
            // New card or no card
            _hoveredCard = cardUnderMouse;
            _hoverTime = 0f;
        }
    }
}
