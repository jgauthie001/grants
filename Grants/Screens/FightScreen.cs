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
public class FightScreen : GameScreen
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

    // Round log for display
    private List<string> _roundLog = new();
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

    private const float HexSize = 36f;
    private const float BoardOriginX = 640f;
    private const float BoardOriginY = 360f;

    public override void OnEnter(object? data = null)
    {
        _font = Game.DefaultFont;
        _smallFont = Game.SmallFont;
        _pixel = Game.Pixel;

        var (fighterDef, matchType) = ((FighterDefinition, string))data!;
        _matchType = matchType;

        // Create fighter instances
        var playerFighter = new FighterInstance(fighterDef, "Player");
        var aiFighter = new FighterInstance(GrantsFighter.CreateDefinition(), "CPU Grants");

        // Apply upgrades from progress
        var progress = Game.PlayerProfile.GetOrCreateProgress(fighterDef.Id);
        var tree = GrantsUpgradeTree.Create(); // TODO: look up tree by fighter ID when multiple fighters exist
        UpgradeEngine.ApplyProgressToInstance(playerFighter, progress, tree);

        _match = new MatchState
        {
            MatchType = _matchType switch
            {
                "pve" => MatchType.PvE,
                "pvp_casual" => MatchType.PvpCasual,
                "pvp_ranked" => MatchType.PvpRanked,
                _ => MatchType.PvE,
            },
            FighterA = playerFighter,
            FighterB = aiFighter,
            FighterAIsHuman = true,
            FighterBIsHuman = false,
        };

        // Starting positions
        _match.FighterA.HexQ = Models.Board.HexBoard.FighterAStart.Q;
        _match.FighterA.HexR = Models.Board.HexBoard.FighterAStart.R;
        _match.FighterB.HexQ = Models.Board.HexBoard.FighterBStart.Q;
        _match.FighterB.HexR = Models.Board.HexBoard.FighterBStart.R;

        LoadAvailablePairs();
    }

    private void LoadAvailablePairs()
    {
        // Reset two-step selection
        _selectedGeneric = null;
        var available = _match.FighterA.GetAvailableGenerics();
        _validGenerics = available;
        _genericSelectionIndex = 0;
        _uniqueSelectionIndex = 0;
        _validUniques.Clear();
        _playerCommitted = false;
    }

    public override void Update(GameTime gameTime)
    {
        try
        {
            var keys = Keyboard.GetState();
            var mouse = Mouse.GetState();

            // Update hover detection
            UpdateCardHover(mouse, gameTime);

            if (_match.Phase == MatchPhase.CardSelection && !_playerCommitted)
            {
                if (_selectedGeneric == null)
                {
                    // Step 1: Select generic card
                    if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
                        _genericSelectionIndex = (_genericSelectionIndex - 1 + _validGenerics.Count) % _validGenerics.Count;

                    if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
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
                    if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
                        _uniqueSelectionIndex = (_uniqueSelectionIndex - 1 + _validUniques.Count) % _validUniques.Count;

                    if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
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
                    ExecuteRound();
                }

                if (IsPressed(keys, _prevKeys, Keys.Escape))
                {
                    // Forced movement: can't cancel — use first valid hex
                    _match.ChosenMoveA = _moveForcedMin > 0 && _validMoveHexes.Count > 0
                        ? _validMoveHexes[0]
                        : null;
                    _awaitingMovement = false;
                    ExecuteRound();
                }

                // Click on a valid hex to move there
                if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                {
                    for (int i = 0; i < _validMoveHexes.Count; i++)
                    {
                        var (hx, hy) = Models.Board.HexBoard.HexToPixel(_validMoveHexes[i], HexSize, BoardOriginX, BoardOriginY);
                        if (Vector2.Distance(new Vector2(mouse.X, mouse.Y), new Vector2(hx, hy)) <= HexSize)
                        {
                            _match.ChosenMoveA = _validMoveHexes[i];
                            _awaitingMovement = false;
                            ExecuteRound();
                            break;
                        }
                    }
                }
            }
            else if (_match.Phase == MatchPhase.RoundResult)
            {
                if (IsPressed(keys, _prevKeys, Keys.Enter) || IsPressed(keys, _prevKeys, Keys.Space))
                {
                    _match.Phase = MatchPhase.CardSelection;
                    LoadAvailablePairs();
                }
            }
            else if (_match.Phase == MatchPhase.MatchOver)
            {
                if (IsPressed(keys, _prevKeys, Keys.Enter))
                    HandleMatchEnd();
            }

            if (IsPressed(keys, _prevKeys, Keys.Escape))
                SwitchTo(ScreenType.MainMenu);

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
        _match.SelectedPairB = AiEngine.SelectPair(_match.FighterB, _match.FighterA, _match.Board);
        _playerCommitted = true;
        _selectedGeneric = null;
        _validUniques.Clear();

        // Check if this pair grants movement — if so, let player choose destination
        int maxMovement = _match.FighterA.GetCardMovement(pair.Generic!)
            + _match.FighterA.GetCardMovement(pair.Unique ?? (Models.Cards.CardBase?)pair.Special ?? pair.Generic!);
        int minMovement = pair.EffectiveMinMovement;

        if (maxMovement > 0)
        {
            var pos = new Models.Board.HexCoord(_match.FighterA.HexQ, _match.FighterA.HexR);
            var oppPos = new Models.Board.HexCoord(_match.FighterB.HexQ, _match.FighterB.HexR);
            _match.Board.SetOccupied(oppPos, true);
            _validMoveHexes = Engine.MovementEngine.GetReachableHexes(_match.FighterA, pair, pos, oppPos, _match.Board);
            _match.Board.SetOccupied(oppPos, false);
            _moveForcedMin = minMovement;
            // If forced movement and no valid hexes exist, just execute immediately
            if (_moveForcedMin > 0 && _validMoveHexes.Count == 0)
            {
                _match.ChosenMoveA = null;
                ExecuteRound();
                return;
            }
            _moveSelectionIndex = _moveForcedMin > 0 ? 1 : 0;
            _awaitingMovement = true;
        }
        else
        {
            _match.ChosenMoveA = null;
            ExecuteRound();
        }
    }

    private void ExecuteRound()
    {
        var round = ResolutionEngine.ResolveRound(_match);
        _roundLog = round.Log;
        _match.ChosenMoveA = null;
    }

    private void HandleMatchEnd()
    {
        bool won = _match.Winner == _match.FighterA;
        SwitchTo(ScreenType.PostMatch, (_match, won));
    }

    public override void Draw(GameTime gameTime, SpriteBatch sb)
    {
        try
        {
            sb.Begin();

            DrawBoard(sb);
            DrawDamageStates(sb);
            DrawCardSelection(sb);
            DrawRoundLog(sb);

            if (_match.Phase == MatchPhase.MatchOver)
                DrawMatchOver(sb);

            // Draw tooltip last (on top)
            if (_hoverTime >= HoverDelay && _hoveredCard != null)
                DrawCardTooltip(sb, Mouse.GetState());

            sb.End();
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game_debug.log");
            using (var writer = new StreamWriter(logPath, append: true))
            {
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DRAW ERROR: {ex.Message}");
                writer.WriteLine(ex.StackTrace);
                writer.Flush();
            }
            throw;
        }
    }

    private void DrawBoard(SpriteBatch sb)
    {
        foreach (var cell in _match.Board.AllCells)
        {
            var (px, py) = Models.Board.HexBoard.HexToPixel(cell, HexSize, BoardOriginX, BoardOriginY);

            Color hexColor = Color.DarkSlateGray;
            if (_awaitingMovement)
            {
                // Highlight selected destination (index > 0) in gold
                if (_moveSelectionIndex > 0 && _validMoveHexes[_moveSelectionIndex - 1] == cell)
                    hexColor = Color.Goldenrod;
                // Highlight all valid destinations in green
                else if (_validMoveHexes.Contains(cell))
                    hexColor = new Color(20, 80, 20);
            }

            DrawHex(sb, (int)px, (int)py, (int)HexSize - 2, hexColor);
        }

        // Fighter A position
        var (ax, ay) = Models.Board.HexBoard.HexToPixel(
            new Models.Board.HexCoord(_match.FighterA.HexQ, _match.FighterA.HexR),
            HexSize, BoardOriginX, BoardOriginY);
        DrawHex(sb, (int)ax, (int)ay, (int)HexSize - 4, Color.CornflowerBlue);
        sb.DrawString(_smallFont, "A", new Vector2(ax - 5, ay - 7), Color.White);

        // Fighter B position
        var (bx, by) = Models.Board.HexBoard.HexToPixel(
            new Models.Board.HexCoord(_match.FighterB.HexQ, _match.FighterB.HexR),
            HexSize, BoardOriginX, BoardOriginY);
        DrawHex(sb, (int)bx, (int)by, (int)HexSize - 4, Color.Crimson);
        sb.DrawString(_smallFont, "B", new Vector2(bx - 5, by - 7), Color.White);

        // Movement selection overlay
        if (_awaitingMovement)
        {
            var playerPos = _moveSelectionIndex == 0
                ? new Models.Board.HexCoord(_match.FighterA.HexQ, _match.FighterA.HexR)
                : _validMoveHexes[_moveSelectionIndex - 1];
            var oppPos = new Models.Board.HexCoord(_match.FighterB.HexQ, _match.FighterB.HexR);
            int previewDist = playerPos.DistanceTo(oppPos);

            string stayLabel = _moveSelectionIndex == 0 ? "> Stay" : "  Stay";
            sb.DrawString(_smallFont, "Choose position:", new Vector2(BoardOriginX - 220, BoardOriginY + 150), Color.White);
            sb.DrawString(_smallFont, stayLabel, new Vector2(BoardOriginX - 220, BoardOriginY + 167), _moveSelectionIndex == 0 ? Color.Yellow : Color.Gray);
            sb.DrawString(_smallFont, $"Dist to opponent: {previewDist}", new Vector2(BoardOriginX - 220, BoardOriginY + 184), Color.LightCyan);
            sb.DrawString(_smallFont, "[Left/Right] Cycle  [Enter] Confirm  [Esc] Stay",
                new Vector2(BoardOriginX - 180, BoardOriginY + 210), Color.DimGray);
        }
    }

    private void DrawDamageStates(SpriteBatch sb)
    {
        DrawFighterHealth(sb, _match.FighterA, 20, 20, true);
        DrawFighterHealth(sb, _match.FighterB, 820, 20, false);
    }

    private void DrawFighterHealth(SpriteBatch sb, FighterInstance fighter, int x, int y, bool isPlayer)
    {
        sb.DrawString(_font, fighter.DisplayName, new Vector2(x, y), Color.White);
        int row = 0;
        foreach (var kvp in fighter.LocationStates)
        {
            Color stateColor = kvp.Value.State switch
            {
                Models.Fighter.DamageState.Healthy  => Color.LimeGreen,
                Models.Fighter.DamageState.Bruised  => Color.Yellow,
                Models.Fighter.DamageState.Injured  => Color.Orange,
                Models.Fighter.DamageState.Disabled => Color.Red,
                _ => Color.White,
            };
            string label = $"{kvp.Key}: {kvp.Value.State}";
            sb.DrawString(_smallFont, label, new Vector2(x, y + 22 + row * 16), stateColor);
            row++;
        }

        // Display Elo for ranked matches (player only)
        if (isPlayer && _match.MatchType == MatchType.PvpRanked)
        {
            var progress = Game.PlayerProfile.GetOrCreateProgress(fighter.Definition.Id);
            string eloLabel = $"Elo: {progress.EloRating:F0}";
            sb.DrawString(_smallFont, eloLabel, new Vector2(x, y + 22 + row * 16), Color.Cyan);
        }
    }

    private void DrawCardSelection(SpriteBatch sb)
    {
        if (_match.Phase != MatchPhase.CardSelection || _playerCommitted) return;

        int panelX = 20, panelY = 200;

        if (_selectedGeneric == null)
        {
            // Step 1: Select Generic Card
            sb.DrawString(_font, "Select Generic Card:_pl", new Vector2(panelX, panelY), Color.White);

            for (int i = 0; i < _validGenerics.Count; i++)
            {
                var card = _validGenerics[i];
                bool sel = i == _genericSelectionIndex;
                Color c = sel ? Color.Yellow : Color.LightGray;
                string mvTypeG = card.BaseMovementType switch
                {
                    Models.Cards.MovementType.Approach => ">",
                    Models.Cards.MovementType.Retreat  => "<",
                    Models.Cards.MovementType.Free     => "*",
                    _                                  => "-",
                };
                string mvStrG = card.MaxMovement == 0 ? "-" :
                    card.MinMovement == card.MaxMovement ? $"{mvTypeG}{card.MaxMovement}" :
                    $"{mvTypeG}{card.MinMovement}-{card.MaxMovement}";
                string label = $"{(sel ? ">" : " ")} {card.Name}  [Spd:{card.BaseSpeed:+#;-#;0} Pwr:{card.BasePower} Def:{card.BaseDefense} Mv:{mvStrG}]";
                sb.DrawString(_smallFont, label, new Vector2(panelX, panelY + 24 + i * 18), c);
            }

            sb.DrawString(_smallFont, "[Up/Down] Navigate   [Enter] Select",
                new Vector2(panelX, panelY + 24 + _validGenerics.Count * 18 + 8), Color.DimGray);
        }
        else
        {
            // Step 2: Select Unique/Special Card
            sb.DrawString(_font, $"Select Combo for: {_selectedGeneric.Name}", new Vector2(panelX, panelY), Color.Yellow);

            if (_validUniques.Count == 0)
            {
                sb.DrawString(_smallFont, "No compatible moves available", new Vector2(panelX, panelY + 30), Color.Red);
                sb.DrawString(_smallFont, "[Esc] Go back", new Vector2(panelX, panelY + 50), Color.DimGray);
                return;
            }

            for (int i = 0; i < _validUniques.Count; i++)
            {
                var card = _validUniques[i];
                bool sel = i == _uniqueSelectionIndex;
                Color c = sel ? Color.Yellow : Color.LightGray;
                string cardName = card switch
                {
                    UniqueCard u => u.Name,
                    SpecialCard s => s.Name,
                    _ => "?"
                };
                // Compute effective range and combined movement for this pair
                var tempPair = new CardPair { Generic = _selectedGeneric, Unique = card as UniqueCard, Special = card as SpecialCard };
                string rangeStr = $"{tempPair.EffectiveMinRange}-{tempPair.EffectiveMaxRange}";
                int combinedMinMv = tempPair.EffectiveMinMovement;
                int combinedMaxMv = _match.FighterA.GetCardMovement(_selectedGeneric!)
                    + _match.FighterA.GetCardMovement(card);
                string mvType = tempPair.CombinedMovementType switch
                {
                    Models.Cards.MovementType.Approach => ">",
                    Models.Cards.MovementType.Retreat  => "<",
                    Models.Cards.MovementType.Free     => "*",
                    _                                  => "-",
                };
                string mvStr = combinedMaxMv == 0 ? "-" :
                    combinedMinMv == combinedMaxMv ? $"{mvType}{combinedMaxMv}" :
                    $"{mvType}{combinedMinMv}-{combinedMaxMv}";
                string label = $"{(sel ? ">" : " ")} {cardName}  [Spd:{card.BaseSpeed:+#;-#;0} Pwr:{card.BasePower} Def:{card.BaseDefense} Mv:{mvStr} Rng:{rangeStr}]";
                sb.DrawString(_smallFont, label, new Vector2(panelX, panelY + 24 + i * 18), c);
            }

            sb.DrawString(_smallFont, "[Up/Down] Navigate   [Enter] Commit   [Backspace] Back",
                new Vector2(panelX, panelY + 24 + _validUniques.Count * 18 + 8), Color.DimGray);
        }
    }

    private void DrawRoundLog(SpriteBatch sb)
    {
        if (_roundLog.Count == 0) return;

        try
        {
            int logX = 20, logY = 560;
            sb.DrawString(_smallFont, "Round Log:_pl", new Vector2(logX, logY), Color.White);
            for (int i = 0; i < Math.Min(_roundLog.Count, 10); i++)
            {
                string logEntry = _roundLog[i];
                // Filter to ASCII-only characters that the font supports
                var filtered = new System.Text.StringBuilder();
                foreach (char c in logEntry)
                {
                    if (c >= 32 && c <= 126)  // Printable ASCII range
                        filtered.Append(c);
                    else if (c == '\n' || c == '\t')
                        filtered.Append(' ');  // Replace whitespace with space
                }
                string safeText = filtered.ToString();
                sb.DrawString(_smallFont, safeText, new Vector2(logX, logY + 16 + i * 14), Color.LightGray);
            }

            if (_match.Phase == MatchPhase.RoundResult)
                sb.DrawString(_smallFont, "[Enter/Space] Next Round",
                    new Vector2(logX, logY + 16 + Math.Min(_roundLog.Count, 10) * 14), Color.DimGray);
        }
        catch
        {
            // If anything goes wrong, just skip the round log display
        }
    }

    private void DrawMatchOver(SpriteBatch sb)
    {
        int cx = Game.GraphicsDevice.Viewport.Width / 2;
        int cy = Game.GraphicsDevice.Viewport.Height / 2;
        bool won = _match.Winner == _match.FighterA;
        string result = won ? "You Win!_pl" : "You Lose!_pl";
        Color col = won ? Color.Gold : Color.OrangeRed;
        var sz = _font.MeasureString(result);
        sb.DrawString(_font, result, new Vector2(cx - sz.X / 2, cy - 20), col);
        sb.DrawString(_smallFont, "[Enter] Continue",
            new Vector2(cx - _smallFont.MeasureString("[Enter] Continue").X / 2, cy + 20), Color.White);
    }

    // Simple filled hex approximation using filled rectangle + rotated quads is complex in MB,
    // so we draw a filled square as placeholder for now
    private void DrawHex(SpriteBatch sb, int cx, int cy, int size, Color color)
    {
        sb.Draw(_pixel, new Rectangle(cx - size / 2, cy - size / 2, size, size), color * 0.6f);
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

    private void DrawCardTooltip(SpriteBatch sb, MouseState mouse)
    {
        if (_hoveredCard == null) return;

        var tooltipLines = CardTooltip.GetCardTooltip(_hoveredCard);
        const int padding = 8;
        const int lineHeight = 14;
        int maxWidth = tooltipLines.Max(line => line.Length) * 8;  // ~8 pixels per char
        int tooltipWidth = maxWidth + padding * 2;
        int tooltipHeight = tooltipLines.Count * lineHeight + padding * 2;

        // Position tooltip near mouse, but keep it on screen
        int tooltipX = mouse.X + 15;
        int tooltipY = mouse.Y + 15;

        int screenWidth = Game.GraphicsDevice.Viewport.Width;
        int screenHeight = Game.GraphicsDevice.Viewport.Height;

        if (tooltipX + tooltipWidth > screenWidth)
            tooltipX = screenWidth - tooltipWidth - 5;
        if (tooltipY + tooltipHeight > screenHeight)
            tooltipY = screenHeight - tooltipHeight - 5;

        // Draw background
        sb.Draw(_pixel, new Rectangle(tooltipX, tooltipY, tooltipWidth, tooltipHeight), Color.Black * 0.85f);
        // Draw border
        DrawRect(sb, tooltipX - 1, tooltipY - 1, tooltipWidth + 2, tooltipHeight + 2, Color.Gold);

        // Draw text
        int textY = tooltipY + padding;
        foreach (var line in tooltipLines)
        {
            Color lineColor = Color.White;
            // Highlight keywords
            if (line.Contains(":")
                && (line.Contains("Bleed") || line.Contains("Break") || line.Contains("Piercing")
                    || line.Contains("Crushing") || line.Contains("Feint") || line.Contains("Quickstep")
                    || line.Contains("Lunge") || line.Contains("Stagger") || line.Contains("Disrupt")
                    || line.Contains("Knockback") || line.Contains("Guard") || line.Contains("Parry")
                    || line.Contains("Deflect") || line.Contains("Sidestep") || line.Contains("Press")
                    || line.Contains("Retreat") || line.Contains("Kill")))
            {
                lineColor = Color.LimeGreen;
            }

            sb.DrawString(_smallFont, line, new Vector2(tooltipX + padding, textY), lineColor);
            textY += lineHeight;
        }
    }

    private void DrawRect(SpriteBatch sb, int x, int y, int width, int height, Color color)
    {
        // Draw border using pixel lines
        sb.Draw(_pixel, new Rectangle(x, y, width, 1), color);          // Top
        sb.Draw(_pixel, new Rectangle(x, y + height - 1, width, 1), color);  // Bottom
        sb.Draw(_pixel, new Rectangle(x, y, 1, height), color);         // Left
        sb.Draw(_pixel, new Rectangle(x + width - 1, y, 1, height), color);  // Right
    }
}
