using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Grants.Models.Fighter;
using Grants.Models.Stage;

namespace Grants.Screens;

/// <summary>
/// Stage selection screen. Shown after fighter selection; player picks an arena.
/// Passes the original fighter data + the chosen stage to FightScreen.
/// </summary>
public class StageSelectScreen : GameScreen
{
    private SpriteFont _font = null!;
    private SpriteFont _smallFont = null!;

    // The fight data forwarded from FighterSelectScreen (opaque — passed straight through)
    private object? _fightData;

    private int _selectedIndex = 0;
    private KeyboardState _prevKeys;

    private static readonly StageModifier[] Stages = new StageModifier[]
    {
        StandardStage.Instance,
        EntryWayStage.Instance,
        ExhibitionStage.Instance,
        ShrinkingArenaStage.Instance,
        PushZoneStage.Instance,
        HazardZoneStage.Instance,
    };

    public override void OnEnter(object? data = null)
    {
        _font = Game.DefaultFont;
        _smallFont = Game.SmallFont;
        _prevKeys = Keyboard.GetState();
        _fightData = data;
        _selectedIndex = 0;
    }

    public override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
            _selectedIndex = (_selectedIndex - 1 + Stages.Length) % Stages.Length;

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
            _selectedIndex = (_selectedIndex + 1) % Stages.Length;

        if (IsPressed(keys, _prevKeys, Keys.Enter))
            Confirm();

        if (IsPressed(keys, _prevKeys, Keys.Escape))
            SwitchTo(ScreenType.FighterSelect);

        _prevKeys = keys;
    }

    private void Confirm()
    {
        var stage = Stages[_selectedIndex];

        // Bundle the fighter data with the chosen stage and forward to FightScreen.
        // FightScreen distinguishes PvP-local by the original tuple shape.
        object fightPayload = _fightData switch
        {
            ValueTuple<FighterDefinition, FighterDefinition, string> pvp
                => (pvp.Item1, pvp.Item2, pvp.Item3, stage),
            ValueTuple<FighterDefinition, string> pve
                => (pve.Item1, pve.Item2, stage),
            _ => _fightData!,
        };

        SwitchTo(ScreenType.Fight, fightPayload);
    }

    public override void Draw(GameTime gameTime, SpriteBatch sb)
    {
        sb.Begin();

        int cx = Game.GraphicsDevice.Viewport.Width / 2;
        int cy = Game.GraphicsDevice.Viewport.Height / 2;

        string title = "Select Stage";
        sb.DrawString(_font, title,
            new Vector2(cx - _font.MeasureString(title).X / 2, 60), Color.White);

        int rowH = 72;
        int startY = cy - (Stages.Length * rowH) / 2;

        for (int i = 0; i < Stages.Length; i++)
        {
            var stage = Stages[i];
            bool sel = i == _selectedIndex;
            int y = startY + i * rowH;

            Color nameColor = sel ? Color.Yellow : Color.White;
            string prefix = sel ? "> " : "  ";

            sb.DrawString(_font,      $"{prefix}{stage.Name}",
                new Vector2(200, y), nameColor);
            sb.DrawString(_smallFont, AsciiOnly(stage.Description),
                new Vector2(222, y + 24), sel ? Color.LightGray : Color.Gray);
        }

        string footer = "[Up/Down] Navigate   [Enter] Select   [Esc] Back";
        sb.DrawString(_smallFont, footer,
            new Vector2(cx - _smallFont.MeasureString(footer).X / 2,
                Game.GraphicsDevice.Viewport.Height - 30), Color.DimGray);

        sb.End();
    }

    private static bool IsPressed(KeyboardState cur, KeyboardState prev, Keys key) =>
        cur.IsKeyDown(key) && prev.IsKeyUp(key);

    private static string AsciiOnly(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (c >= 32 && c <= 126) sb.Append(c);
            else if (c == '\u2014' || c == '\u2013') sb.Append('-'); // em/en dash → hyphen
            else sb.Append(' ');
        }
        return sb.ToString();
    }
}
