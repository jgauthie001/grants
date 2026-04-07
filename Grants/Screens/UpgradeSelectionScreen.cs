using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Grants.Engine;
using Grants.Fighters.Grants;
using Grants.Models.Upgrades;

namespace Grants.Screens;

/// <summary>
/// Post-match upgrade selection screen. Player spends earned upgrade points
/// to unlock nodes from the upgrade tree.
/// Data: (fighterId: string, pointsEarned: int)
/// </summary>
public class UpgradeSelectionScreen : GameScreen
{
    private SpriteFont _font = null!;
    private SpriteFont _smallFont = null!;
    private Texture2D _pixel = null!;

    private string _fighterId = string.Empty;
    private int _pointsEarned = 0;
    private UpgradeTree _tree = null!;
    private FighterProgress _progress = null!;

    // Navigation: list of available node IDs
    private List<string> _availableNodeIds = new();
    private int _selectedIndex = 0;
    private KeyboardState _prevKeys;

    public override void OnEnter(object? data = null)
    {
        _font = Game.DefaultFont;
        _smallFont = Game.SmallFont;
        _pixel = Game.Pixel;

        var (fighterId, pointsEarned) = ((string, int))data!;
        _fighterId = fighterId;
        _pointsEarned = pointsEarned;

        _progress = Game.PlayerProfile.GetOrCreateProgress(_fighterId);
        _tree = GrantsUpgradeTree.Create(); // TODO: look up tree by fighter ID

        // Build list of available upgrades (not already unlocked, and prerequisites met)
        _availableNodeIds.Clear();
        foreach (var (nodeId, node) in _tree.Nodes)
        {
            if (!_progress.UnlockedNodes.Contains(nodeId) && _tree.IsAvailable(nodeId, _progress))
            {
                _availableNodeIds.Add(nodeId);
            }
        }

        // Sort by cost for logical progression
        _availableNodeIds.Sort((a, b) => 
            (_tree.GetNode(a)?.Cost ?? 0).CompareTo(_tree.GetNode(b)?.Cost ?? 0));

        _selectedIndex = 0;
    }

    public override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
            _selectedIndex = (_selectedIndex - 1 + _availableNodeIds.Count) % _availableNodeIds.Count;

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
            _selectedIndex = (_selectedIndex + 1) % _availableNodeIds.Count;

        if (IsPressed(keys, _prevKeys, Keys.Enter) && _availableNodeIds.Count > 0)
            TryUnlockSelected();

        if (IsPressed(keys, _prevKeys, Keys.Escape))
            ReturnToMenu();

        _prevKeys = keys;
    }

    private void TryUnlockSelected()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _availableNodeIds.Count) return;

        string nodeId = _availableNodeIds[_selectedIndex];
        var node = _tree.GetNode(nodeId);
        if (node == null) return;

        if (_progress.TryUnlockNode(node))
        {
            UpgradeEngine.SaveProfile(Game.PlayerProfile);
            // Remove from available list after unlocking
            _availableNodeIds.Remove(nodeId);
            _selectedIndex = Math.Min(_selectedIndex, Math.Max(0, _availableNodeIds.Count - 1));
        }
    }

    private void ReturnToMenu()
    {
        SwitchTo(ScreenType.MainMenu);
    }

    public override void Draw(GameTime gameTime, SpriteBatch sb)
    {
        sb.Begin();

        DrawHeader(sb);
        DrawUpgradeList(sb);
        DrawNodeDetail(sb);
        DrawFooter(sb);

        sb.End();
    }

    private void DrawHeader(SpriteBatch sb)
    {
        sb.DrawString(_font, "Upgrades Earned!_pl", new Vector2(20, 15), Color.LimeGreen);
        
        string earningsInfo = $"Earned {_pointsEarned} points | Available: {_progress.AvailablePoints} | Spent: {_progress.SpentPoints} | Total wins: {_progress.TotalWins}";
        sb.DrawString(_smallFont, earningsInfo, new Vector2(20, 50), Color.LightGray);

        if (_availableNodeIds.Count == 0)
        {
            sb.DrawString(_smallFont, "No upgrades available right now.", new Vector2(20, 75), Color.Yellow);
        }
    }

    private void DrawUpgradeList(SpriteBatch sb)
    {
        int x = 20, y = 100;
        int maxItems = 12;

        sb.DrawString(_smallFont, "Available Upgrades:", new Vector2(x, y - 20), Color.White);

        for (int i = 0; i < Math.Min(_availableNodeIds.Count, maxItems); i++)
        {
            string nodeId = _availableNodeIds[i];
            var node = _tree.GetNode(nodeId);
            if (node == null) continue;

            bool selected = i == _selectedIndex;
            Color color = selected ? Color.Yellow : Color.LightGray;

            string prefix = selected ? "> " : "  ";
            string cost = $"[{node.Cost}pt]";
            string label = $"{prefix}{node.Name,-25} {cost,-8} +{node.PowerRatingValue}pwr";

            sb.DrawString(_smallFont, label, new Vector2(x, y), color);

            if (selected)
                sb.Draw(_pixel, new Rectangle(x, y, 4, 14), Color.Yellow);

            y += 18;
        }

        if (_availableNodeIds.Count > maxItems)
        {
            sb.DrawString(_smallFont, $"... and {_availableNodeIds.Count - maxItems} more", new Vector2(x, y), Color.DimGray);
        }
    }

    private void DrawNodeDetail(SpriteBatch sb)
    {
        if (_availableNodeIds.Count == 0) return;

        var node = _tree.GetNode(_availableNodeIds[_selectedIndex]);
        if (node == null) return;

        int dx = 420, dy = 100;

        // Panel background
        sb.Draw(_pixel, new Rectangle(dx - 10, dy - 10, 420, 350), Color.DarkSlateGray * 0.6f);
        DrawRect(sb, dx - 10, dy - 10, 420, 350, Color.Gold);

        // Title and cost
        sb.DrawString(_font, node.Name, new Vector2(dx, dy), Color.LimeGreen);
        dy += 24;

        string costLine = $"Cost: {node.Cost} points | Power: +{node.PowerRatingValue}";
        sb.DrawString(_smallFont, costLine, new Vector2(dx, dy), 
            _progress.AvailablePoints >= node.Cost ? Color.LimeGreen : Color.Red);
        dy += 20;

        sb.DrawString(_smallFont, $"Branch: {node.Branch}", new Vector2(dx, dy), Color.LightGray);
        dy += 18;

        // Prerequisites
        if (node.Prerequisites.Count > 0)
        {
            sb.DrawString(_smallFont, $"Requires: {string.Join(", ", node.Prerequisites)}", 
                new Vector2(dx, dy), Color.Orange);
            dy += 18;
        }

        dy += 8;

        // Description
        var words = node.Description.Split(' ');
        string line = "";
        foreach (var word in words)
        {
            if ((line + word).Length > 50 && line.Length > 0)
            {
                sb.DrawString(_smallFont, line.TrimEnd(), new Vector2(dx, dy), Color.LightCyan);
                dy += 15;
                line = word + " ";
            }
            else
            {
                line += word + " ";
            }
        }
        if (line.Length > 0)
        {
            sb.DrawString(_smallFont, line.TrimEnd(), new Vector2(dx, dy), Color.LightCyan);
            dy += 15;
        }

        dy += 12;

        // Action prompt
        if (_progress.AvailablePoints >= node.Cost)
        {
            sb.DrawString(_smallFont, "[Enter] Unlock this upgrade", new Vector2(dx, dy), Color.Yellow);
        }
        else
        {
            int needed = node.Cost - _progress.AvailablePoints;
            sb.DrawString(_smallFont, $"Need {needed} more points", new Vector2(dx, dy), Color.Red);
        }
    }

    private void DrawFooter(SpriteBatch sb)
    {
        int vy = Game.GraphicsDevice.Viewport.Height;
        string footer = "[Up/Down] Navigate | [Enter] Unlock | [Esc] Done";
        var footerSize = _smallFont.MeasureString(footer);
        sb.DrawString(_smallFont, footer,
            new Vector2((Game.GraphicsDevice.Viewport.Width - (int)footerSize.X) / 2, vy - 30),
            Color.DimGray);
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
