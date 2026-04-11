using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Grants.Fighters.Grants;
using Grants.Models.Fighter;

namespace Grants.Screens;

/// <summary>
/// Fighter selection screen. Lists available fighters with their ranked unlock status.
/// Passes (FighterDefinition, matchType) to FightScreen.
/// Stub — expandable as more fighters are added.
/// </summary>
public class FighterSelectScreen : GameScreen
{
    private SpriteFont _font = null!;
    private SpriteFont _smallFont = null!;

    private string _matchType = "pve";
    private int _selectedIndex = 0;
    private KeyboardState _prevKeys;

    private List<FighterDefinition> _fighters = new();

    // Local PvP: two-step selection
    private FighterDefinition? _p1Selection = null;
    private bool _selectingP2 = false;

    public override void OnEnter(object? data = null)
    {
        _font = Game.DefaultFont;
        _smallFont = Game.SmallFont;
        _matchType = data as string ?? "pve";
        _p1Selection = null;
        _selectingP2 = false;
        _selectedIndex = 0;

        _fighters = new List<FighterDefinition>
        {
            GrantsFighter.CreateDefinition(),
            // Add more fighters here as they are created
        };
    }

    public override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
            _selectedIndex = (_selectedIndex - 1 + _fighters.Count) % _fighters.Count;

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
            _selectedIndex = (_selectedIndex + 1) % _fighters.Count;

        if (IsPressed(keys, _prevKeys, Keys.Enter))
            Select();

        if (IsPressed(keys, _prevKeys, Keys.Escape))
            SwitchTo(ScreenType.MainMenu);

        _prevKeys = keys;
    }

    private void Select()
    {
        var fighter = _fighters[_selectedIndex];
        var progress = Game.PlayerProfile.GetOrCreateProgress(fighter.Id);

        if (_matchType == "pvp_ranked" && !progress.IsRankedUnlocked)
            return; // Locked — do nothing (UI shows the requirement)

        if (_matchType == "pvp_local" && !_selectingP2)
        {
            // P1 has picked — now ask P2
            _p1Selection = fighter;
            _selectingP2 = true;
            _selectedIndex = 0;
            return;
        }

        if (_matchType == "pvp_local" && _selectingP2)
        {
            SwitchTo(ScreenType.Fight, (_p1Selection!, fighter, "pvp_local"));
            return;
        }

        SwitchTo(ScreenType.Fight, (fighter, _matchType));
    }

    public override void Draw(GameTime gameTime, SpriteBatch sb)
    {
        sb.Begin();
        int cx = Game.GraphicsDevice.Viewport.Width / 2;

        sb.DrawString(_font, _selectingP2 ? "Player 2 - Select Fighter" : "Select Fighter_pl",
            new Vector2(cx - _font.MeasureString(_selectingP2 ? "Player 2 - Select Fighter" : "Select Fighter_pl").X / 2, 60), Color.White);

        for (int i = 0; i < _fighters.Count; i++)
        {
            var f = _fighters[i];
            var prog = Game.PlayerProfile.GetOrCreateProgress(f.Id);
            bool sel = i == _selectedIndex;
            bool rankedLocked = _matchType == "pvp_ranked" && !prog.IsRankedUnlocked;

            Color nameColor = rankedLocked ? Color.DimGray : (sel ? Color.Yellow : Color.White);
            string prefix = sel ? "> " : "  ";
            string rankStr = _matchType == "pvp_ranked"
                ? (prog.IsRankedUnlocked ? "[Ranked Unlocked]" : $"[{prog.TotalWins}/15 wins]")
                : $"[{prog.TotalWins} wins | PR: {prog.PowerRating}]";

            sb.DrawString(_font, $"{prefix}{f.Name}", new Vector2(200, 160 + i * 60), nameColor);
            sb.DrawString(_smallFont, rankStr, new Vector2(450, 168 + i * 60),
                rankedLocked ? Color.Red : Color.LightGray);
            sb.DrawString(_smallFont, f.Description, new Vector2(200, 185 + i * 60), Color.Gray);
        }

        string footer = "[Up/Down] Navigate   [Enter] Select   [Esc] Back";
        sb.DrawString(_smallFont, footer,
            new Vector2(cx - _smallFont.MeasureString(footer).X / 2,
                Game.GraphicsDevice.Viewport.Height - 30), Color.DimGray);

        sb.End();
    }

    private static bool IsPressed(KeyboardState cur, KeyboardState prev, Keys key) =>
        cur.IsKeyDown(key) && prev.IsKeyUp(key);
}
