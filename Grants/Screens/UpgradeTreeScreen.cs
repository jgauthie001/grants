using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Grants.Fighters.Grants;
using Grants.Models.Upgrades;

namespace Grants.Screens;

/// <summary>
/// Browse all upgrade slots organised by card. Shows unlock status and progress.
/// Data: fighterId (string)
/// </summary>
public class UpgradeTreeScreen : GameScreen
{
    private SpriteFont _font = null!;
    private SpriteFont _smallFont = null!;
    private Texture2D _pixel = null!;

    private string _fighterId = string.Empty;
    private FighterUpgradeDef _upgradeDef = null!;
    private FighterProgress _progress = null!;

    // Flat ordered list of (cardId, slot) for navigation
    private List<CardUpgradeSlotDef> _allSlots = new();
    private int _selectedIndex = 0;
    private KeyboardState _prevKeys;

    public override void OnEnter(object? data = null)
    {
        _font = Game.DefaultFont;
        _smallFont = Game.SmallFont;
        _pixel = Game.Pixel;
        _prevKeys = Keyboard.GetState();

        _fighterId = data as string ?? GrantsFighter.FighterId;
        _progress = Game.PlayerProfile.GetOrCreateProgress(_fighterId);

        // TODO: look up upgradeDef by fighter ID when more fighters are added
        _upgradeDef = GrantsUpgrades.Create();

        // Build display order: all slots grouped by card ID, ordered by SlotIndex
        _allSlots = _upgradeDef.Slots.Values
            .OrderBy(s => s.CardId)
            .ThenBy(s => s.SlotIndex)
            .ToList();

        _selectedIndex = Math.Clamp(_selectedIndex, 0, Math.Max(0, _allSlots.Count - 1));
    }

    public override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
            _selectedIndex = (_selectedIndex - 1 + _allSlots.Count) % _allSlots.Count;

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
            _selectedIndex = (_selectedIndex + 1) % _allSlots.Count;

        // Jump to next card (skip remaining slots for current card)
        if (IsPressed(keys, _prevKeys, Keys.Right) || IsPressed(keys, _prevKeys, Keys.D))
        {
            if (_allSlots.Count > 0)
            {
                string curCard = _allSlots[_selectedIndex].CardId;
                int next = _allSlots.FindIndex(_selectedIndex + 1, s => s.CardId != curCard);
                if (next < 0) next = _allSlots.FindIndex(s => s.CardId != curCard);
                if (next >= 0) _selectedIndex = next;
            }
        }

        if (IsPressed(keys, _prevKeys, Keys.Left) || IsPressed(keys, _prevKeys, Keys.A))
        {
            if (_allSlots.Count > 0)
            {
                string curCard = _allSlots[_selectedIndex].CardId;
                int idx = _allSlots.FindLastIndex(_selectedIndex - 1 >= 0 ? _selectedIndex - 1 : _allSlots.Count - 1,
                    s => s.CardId != curCard);
                if (idx >= 0)
                {
                    // Jump to first slot of that card
                    string prevCard = _allSlots[idx].CardId;
                    int first = _allSlots.FindIndex(s => s.CardId == prevCard);
                    if (first >= 0) _selectedIndex = first;
                }
            }
        }

        if (IsPressed(keys, _prevKeys, Keys.Escape) || IsPressed(keys, _prevKeys, Keys.Back))
            SwitchTo(ScreenType.MainMenu);

        _prevKeys = keys;
    }

    public override void Draw(GameTime gameTime, SpriteBatch sb)
    {
        sb.Begin();

        int vw = Game.GraphicsDevice.Viewport.Width;

        DrawHeader(sb, vw);
        DrawSlotList(sb);
        if (_allSlots.Count > 0)
            DrawSlotDetail(sb, _allSlots[_selectedIndex], vw);
        DrawFooter(sb, vw);

        sb.End();
    }

    private void DrawHeader(SpriteBatch sb, int vw)
    {
        sb.DrawString(_font, "Upgrade Tree", new Vector2(20, 15), Color.White);

        int unlocked = _progress.UnlockedSlots.Count;
        int total = _upgradeDef.Slots.Count;
        sb.DrawString(_smallFont,
            $"Total wins: {_progress.TotalWins}   Unlocked: {unlocked}/{total}",
            new Vector2(20, 48), Color.LightGray);
    }

    private void DrawSlotList(SpriteBatch sb)
    {
        int x = 20, y = 75;
        string? lastCardId = null;
        int visStart = Math.Max(0, _selectedIndex - 10);

        for (int i = visStart; i < _allSlots.Count && y < 680; i++)
        {
            var slot = _allSlots[i];

            if (slot.CardId != lastCardId)
            {
                string shortId = slot.CardId.Replace(_fighterId + "_", "").ToUpper();
                sb.DrawString(_smallFont, $"  -- {shortId} --", new Vector2(x, y), Color.SlateGray);
                y += 15;
                lastCardId = slot.CardId;
            }

            bool unlocked = _progress.IsSlotUnlocked(slot.SlotId);
            bool available = _upgradeDef.IsSlotAvailable(slot, _progress);
            bool selected = i == _selectedIndex;

            Color c = unlocked  ? Color.LimeGreen
                    : available ? Color.White
                    : Color.DimGray;

            string status = unlocked ? "[U]" : available ? "[A]" : "[ ]";
            string tier   = slot.SlotIndex == 2 ? "M" : (slot.SlotIndex + 1).ToString();
            string prefix = selected ? ">" : " ";
            string label  = $"{prefix} {status} S{tier} {S(slot.Name)}";

            sb.DrawString(_smallFont, label, new Vector2(x + 8, y), selected ? Color.Yellow : c);

            if (selected)
                sb.Draw(_pixel, new Rectangle(x, y, 4, 14), Color.Yellow);

            y += 16;
        }
    }

    private void DrawSlotDetail(SpriteBatch sb, CardUpgradeSlotDef slot, int vw)
    {
        int dx = vw - 420, dy = 75;

        sb.Draw(_pixel, new Rectangle(dx - 10, dy - 10, 430, 320), Color.DarkSlateGray * 0.6f);
        DrawRect(sb, dx - 10, dy - 10, 430, 320, Color.CornflowerBlue);

        bool unlocked = _progress.IsSlotUnlocked(slot.SlotId);
        bool available = _upgradeDef.IsSlotAvailable(slot, _progress);

        Color headColor = unlocked ? Color.LimeGreen : available ? Color.White : Color.LightGray;
        sb.DrawString(_font, S(slot.Name), new Vector2(dx, dy), headColor);
        dy += 26;

        string shortCard = slot.CardId.Replace(_fighterId + "_", "").ToUpper();
        string tier = slot.SlotIndex == 2 ? "Mastery Slot" : $"Slot {slot.SlotIndex + 1}";
        sb.DrawString(_smallFont, $"{shortCard}  |  {tier}", new Vector2(dx, dy), Color.LightGray);
        dy += 20;

        // Effect
        string effectStr = slot.UpgradeType switch
        {
            SlotUpgradeType.PowerBonus        => $"+{slot.StatBonus} Power",
            SlotUpgradeType.DefenseBonus      => $"+{slot.StatBonus} Defense",
            SlotUpgradeType.SpeedBonus        => $"+{slot.StatBonus} Speed",
            SlotUpgradeType.MovementBonus     => $"+{slot.StatBonus} Movement",
            SlotUpgradeType.CooldownReduction => $"-{slot.CooldownReduction} Cooldown",
            SlotUpgradeType.RangeExtension    => $"+{slot.StatBonus} Range",
            SlotUpgradeType.AddKeyword        => $"+ {slot.KeywordAdded} ({slot.KeywordValue})",
            SlotUpgradeType.PersonaUnlock     => $"Passive: {slot.PersonaUnlockId}",
            _ => slot.UpgradeType.ToString(),
        };
        sb.DrawString(_smallFont, effectStr, new Vector2(dx, dy), Color.Gold);
        dy += 20;

        // Unlock condition and progress
        if (slot.Mastery != null)
        {
            sb.DrawString(_smallFont, $"Mastery: {S(slot.Mastery.Description)}", new Vector2(dx, dy), Color.Orange);
            dy += 18;
            int cur = GetMasteryProgress(slot);
            sb.DrawString(_smallFont, $"Progress: {cur} / {slot.Mastery.Target}", new Vector2(dx, dy), Color.LightGray);
        }
        else
        {
            int played = _progress.CardDistinctMatches.GetValueOrDefault(slot.CardId, 0);
            sb.DrawString(_smallFont, $"Unlocks at {slot.DistinctMatchesRequired} matches with this card.", new Vector2(dx, dy), Color.LightGray);
            dy += 18;
            float pct = slot.DistinctMatchesRequired > 0 ? (float)played / slot.DistinctMatchesRequired : 1f;
            sb.DrawString(_smallFont, $"Progress: {played}/{slot.DistinctMatchesRequired}  ({pct:P0})", new Vector2(dx, dy), Color.LightGray);
        }

        dy += 24;

        // Status line
        string stateMsg = unlocked  ? "Already unlocked."
                        : available ? "Available - play a match to lock in."
                        : "Locked.";
        Color stateCol = unlocked ? Color.LimeGreen : available ? Color.Yellow : Color.Gray;
        sb.DrawString(_smallFont, stateMsg, new Vector2(dx, dy), stateCol);
        dy += 22;

        // Description
        WordWrap(sb, S(slot.Description), dx, dy, 400, Color.LightCyan);
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
        string footer = "[Up/Down] Slot | [Left/Right] Card | [Esc] Back";
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
