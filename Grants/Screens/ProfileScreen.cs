using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Grants.Models.Match;
using MatchType = Grants.Models.Match.MatchType;

namespace Grants.Screens;

/// <summary>
/// Displays the player profile: match history and per-fighter stats.
/// </summary>
public class ProfileScreen : GameScreen
{
    private SpriteFont _font = null!;
    private SpriteFont _smallFont = null!;
    private Texture2D _pixel = null!;

    private KeyboardState _prevKeys;
    private int _historyScroll = 0;

    public override void OnEnter(object? data = null)
    {
        _font = Game.DefaultFont;
        _smallFont = Game.SmallFont;
        _pixel = Game.Pixel;
        _historyScroll = 0;
    }

    public override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        if (IsPressed(keys, _prevKeys, Keys.Up) || IsPressed(keys, _prevKeys, Keys.W))
            _historyScroll = Math.Max(0, _historyScroll - 1);

        if (IsPressed(keys, _prevKeys, Keys.Down) || IsPressed(keys, _prevKeys, Keys.S))
            _historyScroll++;

        if (IsPressed(keys, _prevKeys, Keys.Escape))
            SwitchTo(ScreenType.MainMenu);

        _prevKeys = keys;
    }

    public override void Draw(GameTime gameTime, SpriteBatch sb)
    {
        var profile = Game.PlayerProfile;

        sb.Begin();

        sb.DrawString(_font, "Profile_pl", new Vector2(20, 15), Color.White);
        sb.DrawString(_smallFont, $"ID: {profile.PlayerId}   Rating: {profile.MatchmakingRating}", new Vector2(20, 45), Color.LightGray);
        sb.DrawString(_smallFont, "[↑↓] Scroll history   [Esc] Back", new Vector2(20, 65), Color.DimGray);

        // Per-fighter stats
        int y = 95;
        sb.DrawString(_smallFont, "Fighter Stats:_pl", new Vector2(20, y), Color.White);
        y += 18;

        foreach (var (fighterId, progress) in profile.FighterProgress)
        {
            string ranked = progress.IsRankedUnlocked ? " [Ranked ✓]" : "";
            string line = $"  {fighterId}{ranked}  Wins:{progress.TotalWins} (PvE:{progress.PveWins} Casual:{progress.PvpCasualWins})  " +
                          $"Pts:{progress.AvailablePoints}  Power:{progress.PowerRating}  Nodes:{progress.UnlockedNodes.Count}";
            sb.DrawString(_smallFont, line, new Vector2(20, y), Color.LightCyan);
            y += 16;
        }

        if (profile.FighterProgress.Count == 0)
        {
            sb.DrawString(_smallFont, "  No fighter data yet.", new Vector2(20, y), Color.Gray);
            y += 16;
        }

        // Match history
        y += 10;
        sb.DrawString(_smallFont, "Recent Matches:_pl", new Vector2(20, y), Color.White);
        y += 18;

        var matches = profile.RecentMatches;
        int maxVisible = 20;
        int startIdx = Math.Min(_historyScroll, Math.Max(0, matches.Count - maxVisible));

        for (int i = startIdx; i < Math.Min(matches.Count, startIdx + maxVisible); i++)
        {
            var m = matches[i];
            Color c = m.Won ? Color.LimeGreen : Color.OrangeRed;
            string typeLabel = m.MatchType switch
            {
                MatchType.PvE        => "PvE",
                MatchType.PvpCasual  => "Casual",
                MatchType.PvpRanked  => "Ranked",
                _ => "?"
            };
            string line = $"  [{m.PlayedAt:MM/dd HH:mm}] {(m.Won ? "WIN" : "LOSS")} vs {m.OpponentName}  ({typeLabel} · {m.Rounds} rounds · {m.FighterId})";
            sb.DrawString(_smallFont, line, new Vector2(20, y), c);
            y += 15;
        }

        if (matches.Count == 0)
            sb.DrawString(_smallFont, "  No matches played yet.", new Vector2(20, y), Color.Gray);

        sb.End();
    }

    private static bool IsPressed(KeyboardState cur, KeyboardState prev, Keys key) =>
        cur.IsKeyDown(key) && prev.IsKeyUp(key);
}
