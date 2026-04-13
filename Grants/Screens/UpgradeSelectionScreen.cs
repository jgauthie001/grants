using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Grants.Engine;
using Grants.Fighters.Grants;
using Grants.Models.Upgrades;

namespace Grants.Screens;

/// <summary>
/// Shows upgrade slots newly unlocked after a match, plus progress toward
/// upcoming unlocks.
/// Data: (fighterId: string, newlyUnlocked: List&lt;string&gt;)
/// </summary>
public class UpgradeSelectionScreen : GameScreen
{
    private SpriteFont _font = null!;
    private SpriteFont _smallFont = null!;
    private Texture2D _pixel = null!;

    private string _fighterId = string.Empty;
    private List<string> _newlyUnlocked = new();
    private FighterUpgradeDef _upgradeDef = null!;
    private FighterProgress _progress = null!;

    // "Coming soon" slots: locked slots closest to unlocking
    private List<CardUpgradeSlotDef> _nearSlots = new();
    private int _selectedIndex = 0;
    private bool _browsingNear = false;
    private KeyboardState _prevKeys;

    public override void OnEnter(object? data = null)
    {
        _font = Game.DefaultFont;
        _smallFont = Game.SmallFont;
        _pixel = Game.Pixel;
        _prevKeys = Keyboard.GetState();

        (_fighterId, _newlyUnlocked) = ((string, List<string>))data!;
        _progress = Game.PlayerProfile.GetOrCreateProgress(_fighterId);

        // TODO: look up upgradeDef by fighter ID when more fighters are added
        _upgradeDef = GrantsUpgrades.Create();

        // Build list of near-unlock slots (slots 0 and 1 only, locked, closest to unlock)
        _nearSlots = _upgradeDef.Slots.Values
            .Where(s => s.Mastery == null && !_progress.IsSlotUnlocked(s.SlotId))
            .OrderBy(s => s.DistinctMatchesRequired - _progress.CardDistinctMatches.GetValueOrDefault(s.CardId, 0))
            .Take(8)
            .ToList();

        _selectedIndex = 0;
        _browsingNear = _newlyUnlocked.Count == 0;
    }

    public override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        int listCount = _browsingNear ? _nearSlots.Count : _newlyUnlocked.Count;

        if (listCount > 0)
        {
            if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
                _selectedIndex = (_selectedIndex - 1 + listCount) % listCount;

            if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
                _selectedIndex = (_selectedIndex + 1) % listCount;
        }

        if (IsPressed(keys, _prevKeys, Keys.Tab))
        {
            _browsingNear = !_browsingNear;
            _selectedIndex = 0;
        }

        if (IsPressed(keys, _prevKeys, Keys.T))
            SwitchTo(ScreenType.UpgradeTree, _fighterId);

        if (IsPressed(keys, _prevKeys, Keys.D) || IsPressed(keys, _prevKeys, Keys.Enter) || IsPressed(keys, _prevKeys, Keys.Escape))
            SwitchTo(ScreenType.MainMenu);

        _prevKeys = keys;
    }

    public override void Draw(GameTime gameTime, SpriteBatch sb)
    {
        sb.Begin();

        int vw = Game.GraphicsDevice.Viewport.Width;

        DrawHeader(sb, vw);
        DrawUnlockedPanel(sb);
        DrawNearPanel(sb, vw);
        DrawFooter(sb, vw);

        sb.End();
    }

    private void DrawHeader(SpriteBatch sb, int vw)
    {
        string title = _newlyUnlocked.Count > 0 ? "Upgrades Unlocked!" : "Match Progress";
        Color titleColor = _newlyUnlocked.Count > 0 ? Color.LimeGreen : Color.LightGray;
        var titleSz = _font.MeasureString(title);
        sb.DrawString(_font, title, new Vector2((vw - titleSz.X) / 2, 15), titleColor);

        int unlockedTotal = _progress.UnlockedSlots.Count;
        int totalSlots = _upgradeDef.Slots.Count;
        string statLine = $"Total wins: {_progress.TotalWins}   Slots unlocked: {unlockedTotal} / {totalSlots}";
        var statSz = _smallFont.MeasureString(statLine);
        sb.DrawString(_smallFont, statLine, new Vector2((vw - statSz.X) / 2, 48), Color.LightGray);
    }

    private void DrawUnlockedPanel(SpriteBatch sb)
    {
        int x = 20, y = 80;

        string header = _newlyUnlocked.Count > 0
            ? $"Newly Unlocked ({_newlyUnlocked.Count}):"
            : "Nothing new unlocked.";
        Color headerColor = _newlyUnlocked.Count > 0 ? Color.Gold : Color.DimGray;

        bool isActive = !_browsingNear;
        sb.DrawString(_smallFont, header, new Vector2(x, y), isActive ? headerColor : Color.DimGray * 0.7f);
        y += 20;

        for (int i = 0; i < _newlyUnlocked.Count; i++)
        {
            var slot = _upgradeDef.GetSlot(_newlyUnlocked[i]);
            if (slot == null) continue;

            bool selected = isActive && i == _selectedIndex;
            Color c = selected ? Color.Yellow : Color.LimeGreen;
            string prefix = selected ? "> " : "  ";
            string tier = slot.SlotIndex == 2 ? "[Mastery]" : $"[Slot {slot.SlotIndex + 1}]";
            string shortId = slot.CardId.Replace(_fighterId + "_", "").ToUpper();
            string label = $"{prefix}{tier} {S(slot.Name)} - {shortId}";
            sb.DrawString(_smallFont, label, new Vector2(x, y), c);

            if (selected)
                DrawSlotDetail(sb, slot, x + 320, 80);

            y += 18;
        }

        if (_newlyUnlocked.Count == 0)
            sb.DrawString(_smallFont, "  Keep playing to earn upgrades.", new Vector2(x, y), Color.DimGray);
    }

    private void DrawNearPanel(SpriteBatch sb, int vw)
    {
        int x = 20, y = 320;

        bool isActive = _browsingNear;
        sb.DrawString(_smallFont, "Upcoming Unlocks:", new Vector2(x, y), isActive ? Color.White : Color.DimGray * 0.7f);
        y += 20;

        for (int i = 0; i < _nearSlots.Count; i++)
        {
            var slot = _nearSlots[i];
            bool selected = isActive && i == _selectedIndex;
            Color c = selected ? Color.Yellow : Color.LightGray;
            string prefix = selected ? "> " : "  ";

            int played = _progress.CardDistinctMatches.GetValueOrDefault(slot.CardId, 0);
            string shortId = slot.CardId.Replace(_fighterId + "_", "").ToUpper();
            string label = $"{prefix}[Slot {slot.SlotIndex + 1}] {S(slot.Name)} ({shortId}) - {played}/{slot.DistinctMatchesRequired}";
            sb.DrawString(_smallFont, label, new Vector2(x, y), played >= slot.DistinctMatchesRequired ? Color.LimeGreen : c);

            if (selected)
                DrawSlotDetail(sb, slot, x + 320, 320);

            y += 18;
        }
    }

    private void DrawSlotDetail(SpriteBatch sb, CardUpgradeSlotDef slot, int dx, int dy)
    {
        sb.Draw(_pixel, new Rectangle(dx - 8, dy - 8, 400, 220), Color.DarkSlateGray * 0.6f);
        DrawRect(sb, dx - 8, dy - 8, 400, 220, Color.CornflowerBlue);

        sb.DrawString(_font, S(slot.Name), new Vector2(dx, dy), Color.LimeGreen);
        dy += 24;

        string typeStr = slot.UpgradeType switch
        {
            SlotUpgradeType.PowerBonus        => $"+{slot.StatBonus} Power",
            SlotUpgradeType.DefenseBonus      => $"+{slot.StatBonus} Defense",
            SlotUpgradeType.SpeedBonus        => $"+{slot.StatBonus} Speed",
            SlotUpgradeType.MovementBonus     => $"+{slot.StatBonus} Movement",
            SlotUpgradeType.CooldownReduction => $"-{slot.CooldownReduction} Cooldown",
            SlotUpgradeType.RangeExtension    => $"+{slot.StatBonus} Range",
            SlotUpgradeType.AddKeyword        => $"Keyword: {slot.KeywordAdded} ({slot.KeywordValue})",
            SlotUpgradeType.PersonaUnlock     => $"Passive: {slot.PersonaUnlockId}",
            _ => slot.UpgradeType.ToString(),
        };
        sb.DrawString(_smallFont, typeStr, new Vector2(dx, dy), Color.Gold);
        dy += 18;

        // Unlock condition
        if (slot.Mastery != null)
        {
            int cur = GetMasteryProgress(slot);
            sb.DrawString(_smallFont, $"Mastery: {S(slot.Mastery.Description)}", new Vector2(dx, dy), Color.Orange);
            dy += 18;
            sb.DrawString(_smallFont, $"Progress: {cur} / {slot.Mastery.Target}", new Vector2(dx, dy), Color.LightGray);
        }
        else
        {
            int played = _progress.CardDistinctMatches.GetValueOrDefault(slot.CardId, 0);
            sb.DrawString(_smallFont, $"Unlocks after {slot.DistinctMatchesRequired} matches with this card.", new Vector2(dx, dy), Color.LightGray);
            dy += 18;
            sb.DrawString(_smallFont, $"Progress: {played} / {slot.DistinctMatchesRequired}", new Vector2(dx, dy), Color.LightGray);
        }

        dy += 24;
        WordWrap(sb, S(slot.Description), dx, dy, 360, Color.LightCyan);
    }

    private int GetMasteryProgress(CardUpgradeSlotDef slot)
    {
        if (slot.Mastery == null) return 0;
        return slot.Mastery.Type switch
        {
            MasteryConditionType.PlayedInMatches    => _progress.CardDistinctMatches.GetValueOrDefault(slot.CardId, 0),
            MasteryConditionType.LandedHits         => _progress.CardLandedHits.GetValueOrDefault(slot.CardId, 0),
            MasteryConditionType.LandedVsFaster     => _progress.CardLandedVsFaster.GetValueOrDefault(slot.CardId, 0),
            MasteryConditionType.LandedAtRange      => _progress.CardLandedAtRange.GetValueOrDefault(slot.CardId, 0),
            MasteryConditionType.EventCounter       => slot.Mastery.CounterKey != null
                ? _progress.EventCounters.GetValueOrDefault(slot.Mastery.CounterKey, 0) : 0,
            MasteryConditionType.WonMatchWithCard   => _progress.CardWinsWithCard.GetValueOrDefault(slot.CardId, 0),
            MasteryConditionType.WonWithKillingBlow => _progress.CardKillingBlows.GetValueOrDefault(slot.CardId, 0),
            _ => 0,
        };
    }

    private void DrawFooter(SpriteBatch sb, int vw)
    {
        int vy = Game.GraphicsDevice.Viewport.Height;
        string footer = "[Up/Down] Browse | [Tab] Switch Panel | [T] Full Tree | [Enter/D/Esc] Done";
        var sz = _smallFont.MeasureString(footer);
        sb.DrawString(_smallFont, footer, new Vector2((vw - sz.X) / 2, vy - 30), Color.DimGray);
    }

    private void WordWrap(SpriteBatch sb, string text, int x, int y, int maxWidth, Color color)
    {
        var words = text.Split(' ');
        string line = "";
        foreach (var word in words)
        {
            string test = line.Length > 0 ? line + " " + word : word;
            if (_smallFont.MeasureString(test).X > maxWidth && line.Length > 0)
            {
                sb.DrawString(_smallFont, line, new Vector2(x, y), color);
                y += 15;
                line = word;
            }
            else
            {
                line = test;
            }
        }
        if (line.Length > 0)
            sb.DrawString(_smallFont, line, new Vector2(x, y), color);
    }

    private void DrawRect(SpriteBatch sb, int x, int y, int w, int h, Color color)
    {
        sb.Draw(_pixel, new Rectangle(x, y, w, 1), color);
        sb.Draw(_pixel, new Rectangle(x, y + h - 1, w, 1), color);
        sb.Draw(_pixel, new Rectangle(x, y, 1, h), color);
        sb.Draw(_pixel, new Rectangle(x + w - 1, y, 1, h), color);
    }

    private static bool IsPressed(KeyboardState cur, KeyboardState prev, Keys key) =>
        cur.IsKeyDown(key) && prev.IsKeyUp(key);

    private static string S(string? text)
    {
        if (text == null) return "";
        var buf = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (c >= 32 && c <= 126) buf.Append(c);
            else if (c == '\u2013' || c == '\u2014') buf.Append('-');
            else if (c == '\n') buf.Append(' ');
        }
        return buf.ToString();
    }
}
