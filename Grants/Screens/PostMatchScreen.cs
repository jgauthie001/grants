using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Grants.Engine;
using Grants.Fighters.Grants;
using Grants.Models.Fighter;
using Grants.Models.Match;
using Grants.Models.Upgrades;
using MatchType = Grants.Models.Match.MatchType;

namespace Grants.Screens;

/// <summary>
/// Displays match result and navigation options.
/// Processes upgrade progress and transitions to UpgradeSelectionScreen.
/// Data: (MatchState match, bool playerWon)
/// </summary>
public class PostMatchScreen : GameScreen
{
    private SpriteFont _font = null!;
    private SpriteFont _smallFont = null!;

    private MatchState _match = null!;
    private bool _playerWon;
    private string _fighterId = string.Empty;

    private KeyboardState _prevKeys;

    public override void OnEnter(object? data = null)
    {
        _font = Game.DefaultFont;
        _smallFont = Game.SmallFont;
        _prevKeys = Keyboard.GetState();

        (_match, _playerWon) = ((MatchState, bool))data!;
        _fighterId = _match.FighterA.Definition.Id;
    }

    public override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        if (IsPressed(keys, _prevKeys, Keys.U))
        {
            var progress = Game.PlayerProfile.GetOrCreateProgress(_fighterId);

            // Update Elo for ranked matches
            if (_match.MatchType == MatchType.PvpRanked)
                progress.UpdateEloRating(1200.0, _playerWon);

            // Build match result, record it, and auto-unlock newly met slots
            // TODO: look up upgradeDef by fighter ID when more fighters are added
            var upgradeDef = GrantsUpgrades.Create();
            var result = UpgradeEngine.BuildMatchResult(_match, _playerWon);
            var newlyUnlocked = UpgradeEngine.RecordMatchAndUnlock(progress, upgradeDef, result);

            UpgradeEngine.SaveProfile(Game.PlayerProfile);
            SwitchTo(ScreenType.UpgradeSelection, (_fighterId, newlyUnlocked));
        }

        if (IsPressed(keys, _prevKeys, Keys.R))
            SwitchTo(ScreenType.FighterSelect, _match.MatchType switch
            {
                MatchType.PvE        => "pve",
                MatchType.PvpCasual  => "pvp_casual",
                MatchType.PvpRanked  => "pvp_ranked",
                _ => "pve",
            });

        if (IsPressed(keys, _prevKeys, Keys.M) || IsPressed(keys, _prevKeys, Keys.Escape))
            SwitchTo(ScreenType.MainMenu);

        _prevKeys = keys;
    }

    public override void Draw(GameTime gameTime, SpriteBatch sb)
    {
        sb.Begin();

        int cx = Game.GraphicsDevice.Viewport.Width / 2;
        int cy = Game.GraphicsDevice.Viewport.Height / 2;

        string headline = _match.IsDraw ? "Draw!" : (_playerWon ? "Victory!" : "Defeat!");
        Color headColor = _match.IsDraw ? Color.LightYellow : (_playerWon ? Color.Gold : Color.OrangeRed);
        var headSize = _font.MeasureString(headline);
        sb.DrawString(_font, headline, new Vector2(cx - headSize.X / 2, 80), headColor);

        // Round count
        string rounds = $"Rounds fought: {_match.CurrentRound - 1}";
        var rsz = _smallFont.MeasureString(rounds);
        sb.DrawString(_smallFont, rounds, new Vector2(cx - rsz.X / 2, 140), Color.LightGray);

        // Wins
        var progress = Game.PlayerProfile.GetOrCreateProgress(_fighterId);
        string pts = $"Total wins: {progress.TotalWins}   Slots unlocked: {progress.UnlockedSlots.Count}";
        var psz = _smallFont.MeasureString(pts);
        sb.DrawString(_smallFont, pts, new Vector2(cx - psz.X / 2, 170), Color.LightGreen);

        // Elo rating (if ranked)
        if (_match.MatchType == MatchType.PvpRanked)
        {
            string elo = $"Elo Rating: {progress.EloRating:F0}";
            var esz = _smallFont.MeasureString(elo);
            sb.DrawString(_smallFont, elo, new Vector2(cx - esz.X / 2, 190), Color.Cyan);
        }

        // Round history summary
        sb.DrawString(_smallFont, "Round History:_pl", new Vector2(cx - 200, 220), Color.White);
        for (int i = 0; i < Math.Min(_match.History.Count, 8); i++)
        {
            var round = _match.History[i];
            Color oc = round.Outcome switch
            {
                RoundOutcome.FighterAWins => Color.CornflowerBlue,
                RoundOutcome.FighterBWins => Color.Crimson,
                RoundOutcome.BothHit      => Color.Orange,
                RoundOutcome.BothMissed   => Color.Gray,
                _ => Color.LightGray,
            };
            string line = $"Round {round.RoundNumber}: {round.Outcome}";
            sb.DrawString(_smallFont, line, new Vector2(cx - 200, 240 + i * 18), oc);
        }

        // Navigation hints
        sb.DrawString(_smallFont, "[U] Upgrade Tree   [R] Rematch   [M] Main Menu",
            new Vector2(cx - _smallFont.MeasureString("[U] Upgrade Tree   [R] Rematch   [M] Main Menu").X / 2, 580),
            Color.DimGray);

        sb.End();
    }

    private static bool IsPressed(KeyboardState cur, KeyboardState prev, Keys key) =>
        cur.IsKeyDown(key) && prev.IsKeyUp(key);
}
