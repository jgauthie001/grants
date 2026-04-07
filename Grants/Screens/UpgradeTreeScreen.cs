using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Grants.Engine;
using Grants.Fighters.Grants;
using Grants.Models.Upgrades;

namespace Grants.Screens;

/// <summary>
/// Browse and unlock nodes in a fighter's upgrade tree.
/// Data: fighterId (string) — which fighter's tree to display.
/// </summary>
public class UpgradeTreeScreen : GameScreen
{
    private SpriteFont _font = null!;
    private SpriteFont _smallFont = null!;
    private Texture2D _pixel = null!;

    private string _fighterId = string.Empty;
    private UpgradeTree _tree = null!;
    private FighterProgress _progress = null!;

    // Navigation: list of all node IDs in display order
    private List<string> _nodeIds = new();
    private int _selectedIndex = 0;
    private KeyboardState _prevKeys;

    public override void OnEnter(object? data = null)
    {
        _font = Game.DefaultFont;
        _smallFont = Game.SmallFont;
        _pixel = Game.Pixel;

        _fighterId = data as string ?? "grants";
        _progress = Game.PlayerProfile.GetOrCreateProgress(_fighterId);

        // For now there is only one tree. Extend when more fighters are added.
        _tree = GrantsUpgradeTree.Create();

        // Build flat ordering: branches in insertion order, nodes within each branch in order
        _nodeIds.Clear();
        foreach (var (_, nodeList) in _tree.Branches)
            _nodeIds.AddRange(nodeList);
        // Final nodes (may already be in branches, but add any not included)
        foreach (var id in _tree.FinalNodeIds)
            if (!_nodeIds.Contains(id))
                _nodeIds.Add(id);

        _selectedIndex = Math.Min(_selectedIndex, Math.Max(0, _nodeIds.Count - 1));
    }

    public override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
            _selectedIndex = (_selectedIndex - 1 + _nodeIds.Count) % _nodeIds.Count;

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
            _selectedIndex = (_selectedIndex + 1) % _nodeIds.Count;

        if (IsPressed(keys, _prevKeys, Keys.Enter))
            TryUnlock();

        if (IsPressed(keys, _prevKeys, Keys.Escape))
            SwitchTo(ScreenType.MainMenu);

        _prevKeys = keys;
    }

    private void TryUnlock()
    {
        if (_nodeIds.Count == 0) return;
        string nodeId = _nodeIds[_selectedIndex];
        var node = _tree.GetNode(nodeId);
        if (node == null) return;
        if (!_tree.IsAvailable(nodeId, _progress)) return;

        _progress.TryUnlockNode(node);
        UpgradeEngine.SaveProfile(Game.PlayerProfile);
    }

    public override void Draw(GameTime gameTime, SpriteBatch sb)
    {
        sb.Begin();

        int vw = Game.GraphicsDevice.Viewport.Width;

        sb.DrawString(_font, "Upgrade Tree_pl", new Vector2(20, 15), Color.White);
        sb.DrawString(_smallFont,
            $"Points available: {_progress.AvailablePoints}   Total wins: {_progress.TotalWins}   Power rating: {_progress.PowerRating}",
            new Vector2(20, 45), Color.LightGreen);
        sb.DrawString(_smallFont, "[Up/Down] Navigate   [Enter] Unlock   [Esc] Back",
            new Vector2(20, 65), Color.DimGray);

        DrawNodeList(sb);
        DrawNodeDetail(sb);

        sb.End();
    }

    private void DrawNodeList(SpriteBatch sb)
    {
        int x = 20, y = 95;
        string? lastBranch = null;

        for (int i = 0; i < _nodeIds.Count; i++)
        {
            string id = _nodeIds[i];
            var node = _tree.GetNode(id);
            if (node == null) continue;

            // Branch header
            if (node.Branch != lastBranch)
            {
                sb.DrawString(_smallFont, $"-- {node.Branch} --", new Vector2(x, y), Color.Gray);
                y += 16;
                lastBranch = node.Branch;
            }

            bool unlocked   = _progress.UnlockedNodes.Contains(id);
            bool available  = _tree.IsAvailable(id, _progress);
            bool selected   = i == _selectedIndex;

            Color c = unlocked ? Color.LimeGreen
                    : available ? Color.White
                    : Color.DimGray;

            string prefix = selected ? ">" : " ";
            string status = unlocked ? "[O]" : available ? "[ ]" : "[X]";
            string label  = $"{prefix} {status} {node.Name}  (cost:{node.Cost}pt, pwr:{node.PowerRatingValue})";

            sb.DrawString(_smallFont, label, new Vector2(x + 8, y), c);

            if (selected)
                sb.Draw(_pixel, new Rectangle(x, y, 4, 14), Color.Yellow);

            y += 16;
            if (y > 680) break; // clamp to screen
        }
    }

    private void DrawNodeDetail(SpriteBatch sb)
    {
        if (_nodeIds.Count == 0) return;

        var node = _tree.GetNode(_nodeIds[_selectedIndex]);
        if (node == null) return;

        int dx = Game.GraphicsDevice.Viewport.Width - 420;
        int dy = 95;

        sb.Draw(_pixel, new Rectangle(dx - 10, dy - 10, 430, 280), Color.DarkSlateGray * 0.6f);

        sb.DrawString(_font, node.Name, new Vector2(dx, dy), Color.White);
        dy += 24;
        sb.DrawString(_smallFont, $"Type: {node.NodeType}  Branch: {node.Branch}", new Vector2(dx, dy), Color.LightGray);
        dy += 18;
        sb.DrawString(_smallFont, $"Cost: {node.Cost} pt   Power rating: +{node.PowerRatingValue}", new Vector2(dx, dy), Color.LightGray);
        dy += 18;

        if (node.Prerequisites.Count > 0)
        {
            sb.DrawString(_smallFont, $"Requires: {string.Join(", ", node.Prerequisites)}", new Vector2(dx, dy), Color.Orange);
            dy += 18;
        }

        dy += 8;
        // Wrap description manually (simple word-wrap at ~50 chars)
        var words = node.Description.Split(' ');
        string line = string.Empty;
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

        dy += 8;
        bool unlocked = _progress.UnlockedNodes.Contains(node.Id);
        bool available = _tree.IsAvailable(node.Id, _progress);
        string stateMsg = unlocked ? "Already unlocked." : available ? "[Enter] to unlock" : "Prerequisites not met.";
        Color stateCol = unlocked ? Color.LimeGreen : available ? Color.Yellow : Color.Gray;
        sb.DrawString(_smallFont, stateMsg, new Vector2(dx, dy), stateCol);
    }

    private static bool IsPressed(KeyboardState cur, KeyboardState prev, Keys key) =>
        cur.IsKeyDown(key) && prev.IsKeyUp(key);
}
