using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Grants.Screens;

/// <summary>
/// Main menu screen. Options: Play (PvE), PvP Casual, PvP Ranked, Profile, Exit.
/// </summary>
public class MainMenuScreen : GameScreen
{
    private SpriteFont _font = null!;
    private SpriteFont _smallFont = null!;
    private Texture2D _pixel = null!;

    private string[] _menuItems = { "Play (PvE)_pl", "PvP Casual_pl", "PvP Ranked_pl", "Character Builder_pl", "Profile_pl", "Keyword Editor_pl", "Exit_pl" };
    private int _selectedIndex = 0;
    private KeyboardState _prevKeys;

    public override void OnEnter(object? data = null)
    {
        _font = Game.DefaultFont;
        _smallFont = Game.SmallFont;
        _pixel = Game.Pixel;
    }

    public override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
            _selectedIndex = (_selectedIndex - 1 + _menuItems.Length) % _menuItems.Length;

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
            _selectedIndex = (_selectedIndex + 1) % _menuItems.Length;

        if (IsPressed(keys, _prevKeys, Keys.Enter) || IsPressed(keys, _prevKeys, Keys.Space))
            HandleSelection();

        if (IsPressed(keys, _prevKeys, Keys.Escape))
            Game.Exit();

        _prevKeys = keys;
    }

    private void HandleSelection()
    {
        switch (_selectedIndex)
        {
            case 0: SwitchTo(ScreenType.FighterSelect, "pve"); break;
            case 1: SwitchTo(ScreenType.FighterSelect, "pvp_casual"); break;
            case 2: SwitchTo(ScreenType.FighterSelect, "pvp_ranked"); break;
            case 3: SwitchTo(ScreenType.CharacterBuilder); break;
            case 4: SwitchTo(ScreenType.Profile); break;
            case 5: SwitchTo(ScreenType.KeywordEditor); break;
            case 6: Game.Exit(); break;
        }
    }

    public override void Draw(GameTime gameTime, SpriteBatch sb)
    {
        sb.Begin();

        int cx = Game.GraphicsDevice.Viewport.Width / 2;
        int cy = Game.GraphicsDevice.Viewport.Height / 2;

        // Title
        string title = "GRANTS_pl";
        var titleSize = _font.MeasureString(title);
        sb.DrawString(_font, title, new Vector2(cx - titleSize.X / 2, 80), Color.White);

        string subtitle = "A card-based fighter_pl";
        var subSize = _smallFont.MeasureString(subtitle);
        sb.DrawString(_smallFont, subtitle, new Vector2(cx - subSize.X / 2, 120), Color.Gray);

        // Menu items
        int menuStartY = cy - (_menuItems.Length * 40) / 2;
        for (int i = 0; i < _menuItems.Length; i++)
        {
            bool selected = i == _selectedIndex;
            Color color = selected ? Color.Yellow : Color.White;
            string label = selected ? $"> {_menuItems[i]}" : $"  {_menuItems[i]}";
            var size = _font.MeasureString(label);
            sb.DrawString(_font, label, new Vector2(cx - size.X / 2, menuStartY + i * 40), color);
        }

        // Footer
        string footer = "[Up/Down] Navigate   [Enter] Select   [Esc] Quit";
        var footerSize = _smallFont.MeasureString(footer);
        sb.DrawString(_smallFont, footer,
            new Vector2(cx - footerSize.X / 2, Game.GraphicsDevice.Viewport.Height - 30),
            Color.DimGray);

        sb.End();
    }

    private static bool IsPressed(KeyboardState cur, KeyboardState prev, Keys key) =>
        cur.IsKeyDown(key) && prev.IsKeyUp(key);
}
