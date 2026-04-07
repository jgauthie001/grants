using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Grants.Models.Cards;
using Grants.UI;

namespace Grants.Screens;

/// <summary>
/// Keyword editor screen. Browse and edit descriptions for all 20 keywords.
/// Changes can be saved to disk.
/// </summary>
public class KeywordEditorScreen : GameScreen
{
    private SpriteFont _font = null!;
    private SpriteFont _smallFont = null!;
    private Texture2D _pixel = null!;

    private KeywordDescriptionManager _keywordManager = null!;
    private List<CardKeyword> _keywords = new();
    private int _selectedIndex = 0;

    // Edit mode state
    private bool _editingMode = false;
    private string _editBuffer = string.Empty;
    private KeyboardState _prevKeys;

    public override void Initialize(Game1 game)
    {
        base.Initialize(game);
        _keywordManager = new KeywordDescriptionManager();
        _keywords = _keywordManager.GetAllKeywords()
            .Where(k => k != CardKeyword.None)
            .ToList();
    }

    public override void OnEnter(object? data = null)
    {
        _font = Game.DefaultFont;
        _smallFont = Game.SmallFont;
        _pixel = Game.Pixel;
        _selectedIndex = 0;
        _editingMode = false;
    }

    public override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        if (_editingMode)
        {
            HandleEditMode(keys);
        }
        else
        {
            HandleBrowseMode(keys);
        }

        _prevKeys = keys;
    }

    private void HandleBrowseMode(KeyboardState keys)
    {
        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
            _selectedIndex = (_selectedIndex - 1 + _keywords.Count) % _keywords.Count;

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
            _selectedIndex = (_selectedIndex + 1) % _keywords.Count;

        if (IsPressed(keys, _prevKeys, Keys.Enter))
            EnterEditMode();

        if (IsPressed(keys, _prevKeys, Keys.R))
        {
            _keywordManager.ResetToDefaults();
        }

        if (IsPressed(keys, _prevKeys, Keys.T))
        {
            _keywordManager.Save();
        }

        if (IsPressed(keys, _prevKeys, Keys.Back))
            SwitchTo(ScreenType.MainMenu);
    }

    private void HandleEditMode(KeyboardState keys)
    {
        // Simple text editing with backspace and character input
        if (IsPressed(keys, _prevKeys, Keys.Back) && _editBuffer.Length > 0)
            _editBuffer = _editBuffer.Substring(0, _editBuffer.Length - 1);

        // Enter to confirm
        if (IsPressed(keys, _prevKeys, Keys.Enter))
        {
            ConfirmEdit();
            return;
        }

        // Escape to cancel
        if (IsPressed(keys, _prevKeys, Keys.Escape))
        {
            _editingMode = false;
            _editBuffer = string.Empty;
            return;
        }

        // Handle text input (simplified: only printable characters)
        var chars = GetPressedCharacters(keys, _prevKeys);
        foreach (var c in chars)
        {
            if (_editBuffer.Length < 150)  // Limit description length
                _editBuffer += c;
        }
    }

    private void EnterEditMode()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _keywords.Count) return;
        var keyword = _keywords[_selectedIndex];
        _editBuffer = _keywordManager.GetDescription(keyword);
        _editingMode = true;
    }

    private void ConfirmEdit()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _keywords.Count) return;
        var keyword = _keywords[_selectedIndex];
        _keywordManager.SetDescription(keyword, _editBuffer);
        _editingMode = false;
        _editBuffer = string.Empty;
    }

    private List<char> GetPressedCharacters(KeyboardState current, KeyboardState previous)
    {
        var chars = new List<char>();
        var keys = current.GetPressedKeys();

        foreach (var key in keys)
        {
            if (previous.IsKeyUp(key))  // Only new presses
            {
                // Map keys to characters
                char? c = key switch
                {
                    Keys.A => 'a',
                    Keys.B => 'b',
                    Keys.C => 'c',
                    Keys.D => 'd',
                    Keys.E => 'e',
                    Keys.F => 'f',
                    Keys.G => 'g',
                    Keys.H => 'h',
                    Keys.I => 'i',
                    Keys.J => 'j',
                    Keys.K => 'k',
                    Keys.L => 'l',
                    Keys.M => 'm',
                    Keys.N => 'n',
                    Keys.O => 'o',
                    Keys.P => 'p',
                    Keys.Q => 'q',
                    Keys.R => 'r',
                    Keys.S => 's',
                    Keys.T => 't',
                    Keys.U => 'u',
                    Keys.V => 'v',
                    Keys.W => 'w',
                    Keys.X => 'x',
                    Keys.Y => 'y',
                    Keys.Z => 'z',
                    Keys.Space => ' ',
                    Keys.OemComma => ',',
                    Keys.OemPeriod => '.',
                    Keys.OemQuestion => '?',
                    Keys.OemPlus => '+',
                    Keys.OemMinus => '-',
                    Keys.OemPipe => '|',
                    Keys.D0 => '0',
                    Keys.D1 => '1',
                    Keys.D2 => '2',
                    Keys.D3 => '3',
                    Keys.D4 => '4',
                    Keys.D5 => '5',
                    Keys.D6 => '6',
                    Keys.D7 => '7',
                    Keys.D8 => '8',
                    Keys.D9 => '9',
                    _ => null,
                };

                if (c.HasValue)
                    chars.Add(c.Value);
            }
        }

        return chars;
    }

    public override void Draw(GameTime gameTime, SpriteBatch sb)
    {
        sb.Begin();

        int cx = Game.GraphicsDevice.Viewport.Width / 2;
        int viewportWidth = Game.GraphicsDevice.Viewport.Width;
        int viewportHeight = Game.GraphicsDevice.Viewport.Height;

        if (_editingMode)
        {
            DrawEditMode(sb, cx);
        }
        else
        {
            DrawBrowseMode(sb, cx, viewportHeight);
        }

        sb.End();
    }

    private void DrawBrowseMode(SpriteBatch sb, int cx, int viewportHeight)
    {
        // Title
        string title = "KEYWORD EDITOR";
        var titleSize = _font.MeasureString(title);
        sb.DrawString(_font, title, new Vector2(cx - titleSize.X / 2, 20), Color.Yellow);

        // Keywords list
        int startY = 60;
        int lineHeight = 20;
        int listStartX = 50;

        for (int i = 0; i < _keywords.Count; i++)
        {
            var keyword = _keywords[i];
            bool selected = i == _selectedIndex;
            Color color = selected ? Color.Yellow : Color.White;
            string marker = selected ? "> " : "  ";

            string descPreview = _keywordManager.GetDescription(keyword);
            if (descPreview.Length > 90)
                descPreview = descPreview.Substring(0, 87) + "...";

            string line = $"{marker}{keyword,-20} {descPreview}";
            sb.DrawString(_smallFont, line, new Vector2(listStartX, startY + i * lineHeight), color);
        }

        // Footer
        string footer = "[Up/Down] Navigate | [Enter] Edit | [S] Save | [R] Reset | [Backspace] Back";
        var footerSize = _smallFont.MeasureString(footer);
        sb.DrawString(_smallFont, footer,
            new Vector2(cx - footerSize.X / 2, viewportHeight - 40),
            Color.DimGray);
    }

    private void DrawEditMode(SpriteBatch sb, int cx)
    {
        var keyword = _keywords[_selectedIndex];
        int cy = 150;

        // Header
        sb.DrawString(_font, "EDITING KEYWORD", new Vector2(cx - _font.MeasureString("EDITING KEYWORD").X / 2, cy - 40), Color.Yellow);
        sb.DrawString(_font, keyword.ToString(), new Vector2(cx - _font.MeasureString(keyword.ToString()).X / 2, cy), Color.LimeGreen);

        // Edit box background
        int boxX = 50, boxY = cy + 60, boxW = 1180, boxH = 100;
        sb.Draw(_pixel, new Rectangle(boxX, boxY, boxW, boxH), Color.Black * 0.7f);
        DrawRect(sb, boxX - 1, boxY - 1, boxW + 2, boxH + 2, Color.Gold);

        // Cursor and text
        int textX = boxX + 10, textY = boxY + 10;
        sb.DrawString(_smallFont, _editBuffer, new Vector2(textX, textY), Color.White);

        // Blinking cursor
        if ((int)(DateTime.Now.Ticks / 500000000) % 2 == 0)
        {
            var cursorX = textX + _smallFont.MeasureString(_editBuffer).X;
            sb.Draw(_pixel, new Rectangle((int)cursorX, textY, 2, 16), Color.White);
        }

        // Instructions
        int footerY = boxY + boxH + 20;
        sb.DrawString(_smallFont, "[Enter] Save   [Esc] Cancel   [Backspace] Delete", 
            new Vector2(50, footerY), Color.DimGray);
    }

    private void DrawRect(SpriteBatch sb, int x, int y, int width, int height, Color color)
    {
        sb.Draw(_pixel, new Rectangle(x, y, width, 1), color);                    // Top
        sb.Draw(_pixel, new Rectangle(x, y + height - 1, width, 1), color);        // Bottom
        sb.Draw(_pixel, new Rectangle(x, y, 1, height), color);                   // Left
        sb.Draw(_pixel, new Rectangle(x + width - 1, y, 1, height), color);        // Right
    }

    private static bool IsPressed(KeyboardState cur, KeyboardState prev, Keys key) =>
        cur.IsKeyDown(key) && prev.IsKeyUp(key);
}
