using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Grants.Models.Fighter;
using Grants.Models.Match;
using MatchType = Grants.Models.Match.MatchType;

namespace Grants.Screens;

/// <summary>
/// Displays match result, upgrade points earned, and navigation options.
/// Data: (MatchState match, bool playerWon)
/// </summary>
public class PostMatchScreen : GameScreen
{
    private SpriteFont _font = null!;
    private SpriteFont _smallFont = null!;

    private MatchState _match = null!;
    private bool _playerWon;
    private int _previousPoints;
    private int _newPoints;
    private string _fighterId = string.Empty;

    private KeyboardState _prevKeys;

    public override void OnEnter(object? data = null)
    {
        _font = Game.DefaultFont;
        _smallFont = Game.SmallFont;

        (_match, _playerWon) = ((MatchState, bool))data!;
        _fighterId = _match.FighterA.Definition.Id;

        var progress = Game.PlayerProfile.GetOrCreateProgress(_fighterId);
        _previousPoints = progress.AvailablePoints;
        _newPoints = progress.AvailablePoints;

        // Points already recorded by FightScreen before navigating here
        _newPoints = Game.PlayerProfile.GetOrCreateProgress(_fighterId).AvailablePoints;
    }

    public override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();

        if (IsPressed(keys, _prevKeys, Keys.U))
            SwitchTo(ScreenType.UpgradeTree, _fighterId);

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

        string headline = _playerWon ? "Victory!_pl" : "Defeat!_pl";
        Color headColor = _playerWon ? Color.Gold : Color.OrangeRed;
        var headSize = _font.MeasureString(headline);
        sb.DrawString(_font, headline, new Vector2(cx - headSize.X / 2, 80), headColor);

        // Round count
        string rounds = $"Rounds fought: {_match.CurrentRound - 1}";
        var rsz = _smallFont.MeasureString(rounds);
        sb.DrawString(_smallFont, rounds, new Vector2(cx - rsz.X / 2, 140), Color.LightGray);

        // Upgrade points
        var progress = Game.PlayerProfile.GetOrCreateProgress(_fighterId);
        string pts = $"Upgrade points available: {progress.AvailablePoints}  (Total wins: {progress.TotalWins})";
        var psz = _smallFont.MeasureString(pts);
        sb.DrawString(_smallFont, pts, new Vector2(cx - psz.X / 2, 170), Color.LightGreen);

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
