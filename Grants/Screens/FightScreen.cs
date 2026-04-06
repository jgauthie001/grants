using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Grants.Engine;
using Grants.Fighters.Grants;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;
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

    // Card selection state
    private List<CardPair> _availablePairs = new();
    private int _selectedPairIndex = 0;
    private bool _playerCommitted = false;

    // Round log for display
    private List<string> _roundLog = new();
    private KeyboardState _prevKeys;

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
        _availablePairs = _match.FighterA.GetValidPairs();
        _selectedPairIndex = Math.Min(_selectedPairIndex, Math.Max(0, _availablePairs.Count - 1));
        _playerCommitted = false;
    }

    public override void Update(GameTime gameTime)
    {
        if (_match.IsOver) return;

        var keys = Keyboard.GetState();

        if (_match.Phase == MatchPhase.CardSelection && !_playerCommitted)
        {
            if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
                _selectedPairIndex = (_selectedPairIndex - 1 + _availablePairs.Count) % _availablePairs.Count;

            if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
                _selectedPairIndex = (_selectedPairIndex + 1) % _availablePairs.Count;

            if (IsPressed(keys, _prevKeys, Keys.Enter))
                CommitPlayerChoice();
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
    }

    private void CommitPlayerChoice()
    {
        if (_availablePairs.Count == 0) return;

        _match.SelectedPairA = _availablePairs[_selectedPairIndex];
        _match.SelectedPairB = AiEngine.SelectPair(_match.FighterB, _match.FighterA, _match.Board);
        _playerCommitted = true;

        var round = ResolutionEngine.ResolveRound(_match);
        _roundLog = round.Log;
    }

    private void HandleMatchEnd()
    {
        bool won = _match.Winner == _match.FighterA;
        var progress = Game.PlayerProfile.GetOrCreateProgress(_match.FighterA.Definition.Id);

        if (won)
            progress.RecordWin(
                isPve: _match.MatchType == MatchType.PvE,
                isCasualPvp: _match.MatchType == MatchType.PvpCasual);

        UpgradeEngine.SaveProfile(Game.PlayerProfile);
        SwitchTo(ScreenType.PostMatch, (_match, won));
    }

    public override void Draw(GameTime gameTime, SpriteBatch sb)
    {
        sb.Begin();

        DrawBoard(sb);
        DrawDamageStates(sb);
        DrawCardSelection(sb);
        DrawRoundLog(sb);

        if (_match.Phase == MatchPhase.MatchOver)
            DrawMatchOver(sb);

        sb.End();
    }

    private void DrawBoard(SpriteBatch sb)
    {
        foreach (var cell in _match.Board.AllCells)
        {
            var (px, py) = Models.Board.HexBoard.HexToPixel(cell, HexSize, BoardOriginX, BoardOriginY);
            DrawHex(sb, (int)px, (int)py, (int)HexSize - 2, Color.DarkSlateGray);
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
    }

    private void DrawDamageStates(SpriteBatch sb)
    {
        DrawFighterHealth(sb, _match.FighterA, 20, 20);
        DrawFighterHealth(sb, _match.FighterB, 820, 20);
    }

    private void DrawFighterHealth(SpriteBatch sb, FighterInstance fighter, int x, int y)
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
    }

    private void DrawCardSelection(SpriteBatch sb)
    {
        if (_match.Phase != MatchPhase.CardSelection || _playerCommitted) return;

        int panelX = 20, panelY = 200;
        sb.DrawString(_font, "Select your move:_pl", new Vector2(panelX, panelY), Color.White);

        for (int i = 0; i < _availablePairs.Count; i++)
        {
            var pair = _availablePairs[i];
            bool sel = i == _selectedPairIndex;
            Color c = sel ? Color.Yellow : Color.LightGray;

            string genName = pair.Generic?.Name ?? "(standalone)";
            string moveName = pair.Unique?.Name ?? pair.Special?.Name ?? "?";
            string label = $"{(sel ? ">" : " ")} {genName} + {moveName}  [Spd:{pair.CombinedSpeed:+#;-#;0} Pwr:{pair.CombinedPower} Def:{pair.CombinedDefense} Mov:{pair.CombinedMovement}]";
            sb.DrawString(_smallFont, label, new Vector2(panelX, panelY + 24 + i * 18), c);
        }

        sb.DrawString(_smallFont, "[↑↓] Navigate   [Enter] Commit",
            new Vector2(panelX, panelY + 24 + _availablePairs.Count * 18 + 8), Color.DimGray);
    }

    private void DrawRoundLog(SpriteBatch sb)
    {
        if (_roundLog.Count == 0) return;

        int logX = 20, logY = 560;
        sb.DrawString(_smallFont, "Round Log:_pl", new Vector2(logX, logY), Color.White);
        for (int i = 0; i < Math.Min(_roundLog.Count, 10); i++)
            sb.DrawString(_smallFont, _roundLog[i], new Vector2(logX, logY + 16 + i * 14), Color.LightGray);

        if (_match.Phase == MatchPhase.RoundResult)
            sb.DrawString(_smallFont, "[Enter/Space] Next Round",
                new Vector2(logX, logY + 16 + Math.Min(_roundLog.Count, 10) * 14), Color.DimGray);
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
}
