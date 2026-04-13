using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Linq;
using Grants.Engine;
using Grants.Fighters.Grants;
using Grants.Models.Cards;
using Grants.Models.Fighter;
using Grants.Models.Match;
using Grants.UI;
using MatchType = Grants.Models.Match.MatchType;

namespace Grants.Screens;

public partial class FightScreen
{
    public override void Draw(GameTime gameTime, SpriteBatch sb)
    {
        try
        {
            sb.Begin();

            DrawBoard(sb);
            DrawDamageStates(sb);
            DrawCardSelection(sb);
            DrawOpponentCards(sb);
            DrawResolutionStep(sb);
            DrawCardReveal(sb);
            DrawStageChoicePrompt(sb);
            DrawPersonaChoicePrompt(sb);
            DrawPreRoundSelfChoice(sb);
            DrawStageHud(sb);
            DrawPersonaHud(sb);

            if (_match.Phase == MatchPhase.MatchOver)
                DrawMatchOver(sb);

            if (_resignPending)
                DrawResignConfirm(sb);

            // Draw tooltip last (on top)
            if (_hoverTime >= HoverDelay && _hoveredCard != null)
                DrawCardTooltip(sb, Mouse.GetState());

            sb.End();
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game_debug.log");
            using (var writer = new StreamWriter(logPath, append: true))
            {
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DRAW ERROR: {ex.Message}");
                writer.WriteLine(ex.StackTrace);
                writer.Flush();
            }
            throw;
        }
    }

    private void DrawStageHud(SpriteBatch sb)
    {
        var lines = _match.Stage.GetHudDisplayInfo(_match.StageState);
        if (lines.Count == 0) return;

        int screenW = Game.GraphicsDevice.Viewport.Width;
        int y = Game.GraphicsDevice.Viewport.Height - 50;
        // Concatenate all lines centered across the bottom
        string text = string.Join("  |  ", lines);
        float tw = _smallFont.MeasureString(text).X;
        sb.DrawString(_smallFont, text, new Vector2((screenW - tw) / 2f, y), new Color(180, 220, 255));
    }

    private void DrawPersonaChoicePrompt(SpriteBatch sb)
    {
        bool isOpponentChoiceA = _match.Phase == MatchPhase.PersonaChoiceA;
        bool isOpponentChoiceB = _match.Phase == MatchPhase.PersonaChoiceB;
        bool isSelfChoiceA     = _match.Phase == MatchPhase.PersonaSelfChoiceA;
        bool isSelfChoiceB     = _match.Phase == MatchPhase.PersonaSelfChoiceB;

        if (!isOpponentChoiceA && !isOpponentChoiceB && !isSelfChoiceA && !isSelfChoiceB)
            return;

        bool isA = isOpponentChoiceA || isSelfChoiceA;
        string fighterLabel = isA ? (_isLocalPvP ? "Player 1" : "Player") : "Player 2";

        int cx = Game.GraphicsDevice.Viewport.Width / 2;
        int logX = Layout.LeftX, logY = Layout.LogY;

        // Show pre-round log
        for (int i = 0; i < Math.Min(_preRoundLog.Count, 6); i++)
        {
            var filtered = new System.Text.StringBuilder();
            foreach (char c in _preRoundLog[i])
            {
                if (c >= 32 && c <= 126) filtered.Append(c);
                else if (c == '\n' || c == '\t') filtered.Append(' ');
            }
            sb.DrawString(_smallFont, filtered.ToString(), new Vector2(logX, logY + i * 14), new Color(220, 180, 100));
        }

        int promptY = Game.GraphicsDevice.Viewport.Height / 2 - 30;
        string raw;
        string header;
        Color headerColor;

        if (isSelfChoiceA || isSelfChoiceB)
        {
            var ownerFighter = isSelfChoiceA ? _match.FighterA : _match.FighterB;
            var oppFighter   = isSelfChoiceA ? _match.FighterB : _match.FighterA;
            raw = ownerFighter.Definition.Persona.GetSelfChoicePrompt(
                ownerFighter, oppFighter, ownerFighter.PersonaState);
            header     = $"{fighterLabel}: Your persona offers you a choice";
            headerColor = Color.Cyan;
        }
        else
        {
            var promptingOwner   = isOpponentChoiceA ? _match.FighterB : _match.FighterA;
            var respondingFighter = isOpponentChoiceA ? _match.FighterA : _match.FighterB;
            raw = promptingOwner.Definition.Persona.GetOpponentChoicePrompt(
                promptingOwner, respondingFighter, promptingOwner.PersonaState);
            header     = $"{fighterLabel}: Opponent's ability";
            headerColor = Color.Orchid;
        }

        var filteredPrompt = new System.Text.StringBuilder();
        foreach (char c in raw)
        {
            if (c >= 32 && c <= 126) filteredPrompt.Append(c);
            else if (c == '\n' || c == '\t') filteredPrompt.Append(' ');
        }
        string safePrompt = filteredPrompt.ToString();

        string keyHint = "[Y] Accept   [N] Decline";
        float headerW = _font.MeasureString(header).X;
        float promptW = _smallFont.MeasureString(safePrompt).X;
        float keysW   = _smallFont.MeasureString(keyHint).X;
        sb.DrawString(_font,      header,     new Vector2(cx - headerW / 2, promptY),      headerColor);
        sb.DrawString(_smallFont, safePrompt, new Vector2(cx - promptW / 2, promptY + 28), Color.White);
        sb.DrawString(_smallFont, keyHint,    new Vector2(cx - keysW  / 2, promptY + 50),  Color.Yellow);
    }

    private void DrawPersonaHud(SpriteBatch sb)
    {
        var linesA = _match.FighterA.Definition.Persona.GetHudDisplayInfo(_match.FighterA.PersonaState);
        var linesB = _match.FighterB.Definition.Persona.GetHudDisplayInfo(_match.FighterB.PersonaState);
        if (linesA.Count == 0 && linesB.Count == 0) return;

        int screenW = Game.GraphicsDevice.Viewport.Width;
        int y = Game.GraphicsDevice.Viewport.Height - 66;
        var allLines = linesA.Concat(linesB).ToList();
        string text = string.Join("  |  ", allLines.Select(l =>
        {
            var sb2 = new System.Text.StringBuilder();
            foreach (char c in l)
            {
                if (c >= 32 && c <= 126) sb2.Append(c);
                else sb2.Append(' ');
            }
            return sb2.ToString();
        }));
        float tw = _smallFont.MeasureString(text).X;
        sb.DrawString(_smallFont, text, new Vector2((screenW - tw) / 2f, y), new Color(255, 180, 220));
    }

    private void DrawPreRoundSelfChoice(SpriteBatch sb)
    {
        bool isA = _match.Phase == MatchPhase.PreRoundSelfChoiceA;
        bool isB = _match.Phase == MatchPhase.PreRoundSelfChoiceB;
        if ((!isA && !isB) || _preRoundChoice == null) return;

        var owner = isA ? _match.FighterA : _match.FighterB;
        int px = (int)Layout.LeftX;
        int py = 200;

        var (tr, tg, tb) = _preRoundChoice.HeaderTint;
        sb.DrawString(_font, $"{owner.DisplayName}: Choose", new Vector2(px, py), new Color(tr, tg, tb));
        sb.DrawString(_smallFont, _preRoundChoice.Prompt, new Vector2(px, py + 28), Color.LightGray);

        if (_preRoundChoice.Options.Count == 0)
        {
            sb.DrawString(_smallFont, "(No options available)", new Vector2(px, py + 50), Color.Gray);
        }
        else
        {
            for (int i = 0; i < _preRoundChoice.Options.Count; i++)
            {
                var opt = _preRoundChoice.Options[i];
                bool sel = i == _preRoundChoiceIndex;
                var labelSb = new System.Text.StringBuilder();
                foreach (char c in $"{(sel ? ">" : " ")} {opt.Label}") if (c >= 32 && c <= 126) labelSb.Append(c);
                sb.DrawString(_smallFont, labelSb.ToString(), new Vector2(px, py + 52 + i * 32), sel ? Color.Yellow : Color.LightGray);
                if (!string.IsNullOrEmpty(opt.Description))
                {
                    var descSb = new System.Text.StringBuilder();
                    foreach (char c in $"    {opt.Description}") if (c >= 32 && c <= 126) descSb.Append(c);
                    sb.DrawString(_smallFont, descSb.ToString(), new Vector2(px, py + 52 + i * 32 + 14), new Color(120, 120, 120));
                }
            }
        }

        int hintY = py + 52 + _preRoundChoice.Options.Count * 32 + 10;
        string hint = _preRoundChoice.CanSkip
            ? "[Up/Down] Navigate   [Enter] Select   [Backspace] Skip"
            : "[Up/Down] Navigate   [Enter] Select";
        sb.DrawString(_smallFont, hint, new Vector2(px, hintY), Color.DimGray);
    }

    private void DrawStageChoicePrompt(SpriteBatch sb)
    {
        if (_match.Phase != MatchPhase.StageChoiceA && _match.Phase != MatchPhase.StageChoiceB)
            return;

        bool isA = _match.Phase == MatchPhase.StageChoiceA;
        var fighter = isA ? _match.FighterA : _match.FighterB;
        string fighterLabel = isA ? (_isLocalPvP ? "Player 1" : "Player") : "Player 2";

        int cx = Game.GraphicsDevice.Viewport.Width / 2;
        int logX = Layout.LeftX, logY = Layout.LogY;

        // Show pre-round token damage log
        for (int i = 0; i < Math.Min(_preRoundLog.Count, 6); i++)
        {
            var filtered = new System.Text.StringBuilder();
            foreach (char c in _preRoundLog[i])
            {
                if (c >= 32 && c <= 126) filtered.Append(c);
                else if (c == '\n' || c == '\t') filtered.Append(' ');
            }
            sb.DrawString(_smallFont, filtered.ToString(), new Vector2(logX, logY + i * 14), new Color(220, 180, 100));
        }

        // Choice prompt
        int promptY = Game.GraphicsDevice.Viewport.Height / 2 - 30;
        string header  = $"{fighterLabel}: Make your choice";
        string prompt  = _match.Stage.GetChoicePrompt(fighter, _match.StageState);
        string keyHint = "[Y] Yes   [N] No";

        var filteredPrompt = new System.Text.StringBuilder();
        foreach (char c in prompt)
        {
            if (c >= 32 && c <= 126) filteredPrompt.Append(c);
            else if (c == '\n' || c == '\t') filteredPrompt.Append(' ');
        }
        string safePrompt = filteredPrompt.ToString();

        float headerW  = _font.MeasureString(header).X;
        float promptW  = _smallFont.MeasureString(safePrompt).X;
        float keysW    = _smallFont.MeasureString(keyHint).X;

        sb.DrawString(_font,      header,     new Vector2(cx - headerW / 2,  promptY),      Color.Gold);
        sb.DrawString(_smallFont, safePrompt, new Vector2(cx - promptW / 2,  promptY + 28), Color.White);
        sb.DrawString(_smallFont, keyHint,    new Vector2(cx - keysW  / 2,   promptY + 50), Color.Yellow);
    }

    private void DrawBoard(SpriteBatch sb)
    {
        var L = Layout;
        foreach (var cell in _match.Board.AllCells)
        {
            var (px, py) = Models.Board.HexBoard.HexToPixel(cell, L.HexSize, L.BoardCenterX, L.BoardCenterY);

            Color hexColor = Color.DarkSlateGray;
            if (_awaitingMovement)
            {
                // Highlight selected destination (index > 0) in gold
                if (_moveSelectionIndex > 0 && _validMoveHexes[_moveSelectionIndex - 1] == cell)
                    hexColor = Color.Goldenrod;
                // Highlight all valid destinations in green
                else if (_validMoveHexes.Contains(cell))
                    hexColor = new Color(20, 80, 20);
            }

            DrawHex(sb, (int)px, (int)py, (int)L.HexSize - 2, hexColor);
        }

        // Draw stage hazardous hexes (tokens, etc.) â€” orange/gold, rendered under fighters
        var hazardHexes = _match.Stage.GetHazardousHexes(_match.StageState);
        foreach (var hh in hazardHexes)
        {
            if (_match.Board.IsValid(hh))
            {
                var (hpx, hpy) = Models.Board.HexBoard.HexToPixel(hh, L.HexSize, L.BoardCenterX, L.BoardCenterY);
                DrawHex(sb, (int)hpx, (int)hpy, (int)L.HexSize - 6, new Color(200, 140, 40));
            }
        }

        // Draw persona board overlays (e.g. Revenant Witch spirits) â€” rendered under fighters
        foreach (var fighter in new[] { _match.FighterA, _match.FighterB })
        {
            var overlays = fighter.Definition.Persona.GetBoardOverlays(fighter.PersonaState);
            foreach (var ov in overlays)
            {
                if (_match.Board.IsValid(ov.Position))
                {
                    var (ox, oy) = Models.Board.HexBoard.HexToPixel(ov.Position, L.HexSize, L.BoardCenterX, L.BoardCenterY);
                    DrawHex(sb, (int)ox, (int)oy, (int)L.HexSize - 8, new Color((int)ov.R, (int)ov.G, (int)ov.B, (int)ov.A));
                    if (!string.IsNullOrEmpty(ov.Label))
                        sb.DrawString(_smallFont, ov.Label, new Vector2(ox - 4, oy - 7), new Color((int)ov.R, (int)ov.G, (int)ov.B));
                }
            }
        }

        // Fighter A position
        var (ax, ay) = Models.Board.HexBoard.HexToPixel(
            new Models.Board.HexCoord(_match.FighterA.HexQ, _match.FighterA.HexR),
            L.HexSize, L.BoardCenterX, L.BoardCenterY);
        DrawHex(sb, (int)ax, (int)ay, (int)L.HexSize - 4, Color.CornflowerBlue);
        sb.DrawString(_smallFont, "A", new Vector2(ax - 5, ay - 7), Color.White);

        // Fighter B position
        var (bx, by) = Models.Board.HexBoard.HexToPixel(
            new Models.Board.HexCoord(_match.FighterB.HexQ, _match.FighterB.HexR),
            L.HexSize, L.BoardCenterX, L.BoardCenterY);
        DrawHex(sb, (int)bx, (int)by, (int)L.HexSize - 4, Color.Crimson);
        sb.DrawString(_smallFont, "B", new Vector2(bx - 5, by - 7), Color.White);

        // Movement selection overlay
        if (_awaitingMovement)
        {
            var playerPos = _moveSelectionIndex == 0
                ? new Models.Board.HexCoord(_match.FighterA.HexQ, _match.FighterA.HexR)
                : _validMoveHexes[_moveSelectionIndex - 1];
            var oppPos = new Models.Board.HexCoord(_match.FighterB.HexQ, _match.FighterB.HexR);
            int previewDist = playerPos.DistanceTo(oppPos);

            string stayLabel = _moveSelectionIndex == 0 ? "> Stay" : "  Stay";
            sb.DrawString(_smallFont, "Choose position:", new Vector2(L.BoardCenterX - 220, L.BoardCenterY + 150), Color.White);
            sb.DrawString(_smallFont, stayLabel, new Vector2(L.BoardCenterX - 220, L.BoardCenterY + 167), _moveSelectionIndex == 0 ? Color.Yellow : Color.Gray);
            sb.DrawString(_smallFont, $"Dist to opponent: {previewDist}", new Vector2(L.BoardCenterX - 220, L.BoardCenterY + 184), Color.LightCyan);
            sb.DrawString(_smallFont, "[Left/Right] Cycle  [Enter] Confirm  [Esc] Stay",
                new Vector2(L.BoardCenterX - 180, L.BoardCenterY + 210), Color.DimGray);
        }
    }

    private void DrawOpponentCards(SpriteBatch sb)
    {
        var opp = _match.FighterB;
        int x   = Layout.RightX;
        int y   = 200;
        const int GAP = 6;

        sb.DrawString(_font, "Opponent Cards", new Vector2(x, y), Color.LightGray);
        y += 26;

        // Generic cards as widgets
        sb.DrawString(_smallFont, "Body:", new Vector2(x, y), Color.DimGray);
        y += 14;
        foreach (var card in opp.Definition.GenericCards)
        {
            int cd = opp.GetCooldown(card.Id);
            var loc = Models.Fighter.FighterInstance.BodyPartToLocation(card.BodyPart);
            bool avail = opp.LocationStates[loc].State != Models.Fighter.DamageState.Disabled && cd == 0;
            CardWidget.DrawGeneric(sb, _pixel, _font, _smallFont, card, opp,
                x, y, selected: false, available: avail, cooldown: cd);
            y += CardWidget.WIDGET_H + GAP;
        }

        y += 4;
        // Unique cards as widgets
        sb.DrawString(_smallFont, "Moves:", new Vector2(x, y), Color.DimGray);
        y += 14;
        foreach (var card in opp.GetAllUniques())
        {
            int cd    = opp.GetCooldown(card.Id);
            bool avail = cd == 0;
            CardWidget.DrawUnique(sb, _pixel, _font, _smallFont, card, opp,
                x, y, selected: false, available: avail, cooldown: cd, pairContext: null);
            y += CardWidget.WIDGET_H + GAP;
        }

        // Special cards
        foreach (var card in opp.Definition.SpecialCards)
        {
            int cd    = opp.GetCooldown(card.Id);
            bool avail = cd == 0;
            CardWidget.DrawSpecial(sb, _pixel, _font, _smallFont, card, opp,
                x, y, selected: false, available: avail, cooldown: cd);
            y += CardWidget.WIDGET_H + GAP;
        }
    }
    private void DrawDamageStates(SpriteBatch sb)
    {
        DrawFighterHealth(sb, _match.FighterA, 20, 20, true);
        DrawFighterHealth(sb, _match.FighterB, Layout.RightX, 20, false);
    }

    private void DrawFighterHealth(SpriteBatch sb, FighterInstance fighter, int x, int y, bool isPlayer)
    {
        sb.DrawString(_font, fighter.DisplayName, new Vector2(x, y), Color.White);

        // KO condition summary
        int critCount = fighter.Definition.CriticalLocations.Count;
        int threshold = fighter.Definition.KOThreshold;
        string koLine = threshold >= critCount
            ? "KO: all critical locs disabled"
            : $"KO: {threshold}/{critCount} critical locs disabled";
        sb.DrawString(_smallFont, koLine, new Vector2(x, y + 18), new Color(200, 130, 130));

        int row = 0;
        foreach (var kvp in fighter.LocationStates)
        {
            bool isCritical = fighter.Definition.CriticalLocations.Contains(kvp.Key);
            Color stateColor = kvp.Value.State switch
            {
                Models.Fighter.DamageState.Healthy  => Color.LimeGreen,
                Models.Fighter.DamageState.Bruised  => Color.Yellow,
                Models.Fighter.DamageState.Injured  => Color.Orange,
                Models.Fighter.DamageState.Disabled => Color.Red,
                _ => Color.White,
            };
            string critMark = isCritical ? "!" : " ";
            string label = $"{critMark}{kvp.Key}: {kvp.Value.State}";
            sb.DrawString(_smallFont, label, new Vector2(x, y + 36 + row * 16), stateColor);
            row++;
        }

        // Display Elo for ranked matches (player only)
        if (isPlayer && _match.MatchType == MatchType.PvpRanked)
        {
            var progress = Game.PlayerProfile.GetOrCreateProgress(fighter.Definition.Id);
            string eloLabel = $"Elo: {progress.EloRating:F0}";
            sb.DrawString(_smallFont, eloLabel, new Vector2(x, y + 36 + row * 16), Color.Cyan);
        }
    }

    private void DrawCardSelection(SpriteBatch sb)
    {
        if (_match.Phase != MatchPhase.CardSelection) return;

        int screenW = Game.GraphicsDevice.Viewport.Width;
        int screenH = Game.GraphicsDevice.Viewport.Height;
        int cx = screenW / 2;
        int cy = screenH / 2;

        // â”€â”€ Pass-to-P2 overlay â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (_pvpPhase == LocalPvpPhase.PassToP2)
        {
            string msg1 = "Player 1 has chosen.";
            string msg2 = "Pass the controller to Player 2.";
            string msg3 = "[Enter] Ready";
            sb.DrawString(_font,      msg1, new Vector2(cx - _font.MeasureString(msg1).X / 2,      cy - 40), Color.White);
            sb.DrawString(_smallFont, msg2, new Vector2(cx - _smallFont.MeasureString(msg2).X / 2,  cy),      Color.LightGray);
            sb.DrawString(_smallFont, msg3, new Vector2(cx - _smallFont.MeasureString(msg3).X / 2,  cy + 30), Color.Yellow);
            return;
        }

        // â”€â”€ Determine whose turn it is â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        bool isP2 = _pvpPhase == LocalPvpPhase.P2Selecting;
        var fighter      = isP2 ? _match.FighterB : _match.FighterA;
        var availGen     = isP2 ? _p2ValidGenerics : _validGenerics;
        var availUni     = isP2 ? _p2ValidUniques  : _validUniques;
        var selGen       = isP2 ? _p2SelectedGeneric : _selectedGeneric;
        int genIdx       = isP2 ? _p2GenericIndex : _genericSelectionIndex;
        int uniIdx       = isP2 ? _p2UniqueIndex  : _uniqueSelectionIndex;
        string playerLabel = isP2 ? "Player 2" : (_isLocalPvP ? "Player 1" : "Player");
        var defender     = isP2 ? (FighterInstance)_match.FighterA : _match.FighterB;

        // â”€â”€ Header â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        int headerY = 12;
        string headerText = selGen == null
            ? $"{playerLabel} - Choose body card:"
            : $"{playerLabel} - Choose technique for: {AsciiOnly(selGen.Name)}";
        sb.DrawString(_font, headerText, new Vector2(20, headerY), isP2 ? Color.Cyan : Color.White);

        // â”€â”€ Card row layout â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        const int GAP     = 8;
        int rowY          = 40;
        int totalCards    = selGen == null ? availGen.Count : availUni.Count;
        int rowWidth      = totalCards * (CardWidget.WIDGET_W + GAP) - GAP;
        int rowStartX     = Math.Max(20, (screenW - rowWidth) / 2);

        if (selGen == null)
        {
            // â”€â”€ STEP 1: Generic card row â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            for (int i = 0; i < availGen.Count; i++)
            {
                var card   = availGen[i];
                bool sel   = i == genIdx;
                int cd     = fighter.GetCooldown(card.Id);
                int wx     = rowStartX + i * (CardWidget.WIDGET_W + GAP);
                CardWidget.DrawGeneric(sb, _pixel, _font, _smallFont, card, fighter, wx, rowY,
                    selected: sel, available: true, cooldown: cd);
            }

            // â”€â”€ Unavailable generics â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var disabled = fighter.Definition.GenericCards.Where(c => !availGen.Contains(c)).ToList();
            if (disabled.Count > 0)
            {
                int dRowY     = rowY + CardWidget.WIDGET_H + GAP;
                int dRowWidth = disabled.Count * (CardWidget.WIDGET_W + GAP) - GAP;
                int dStartX   = Math.Max(20, (screenW - dRowWidth) / 2);
                for (int i = 0; i < disabled.Count; i++)
                {
                    var card = disabled[i];
                    var loc  = Models.Fighter.FighterInstance.BodyPartToLocation(card.BodyPart);
                    bool dis = fighter.LocationStates[loc].State == Models.Fighter.DamageState.Disabled;
                    int cd   = fighter.GetCooldown(card.Id);
                    int wx   = dStartX + i * (CardWidget.WIDGET_W + GAP);
                    CardWidget.DrawGeneric(sb, _pixel, _font, _smallFont, card, fighter, wx, dRowY,
                        selected: false, available: false, cooldown: cd);
                }
                sb.DrawString(_smallFont, "Unavailable:", new Vector2(dStartX, dRowY - 14), Color.DimGray);
            }

            // â”€â”€ Combo preview strip under selected generic â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            if (availGen.Count > 0)
            {
                var previewed  = availGen[genIdx];
                var compatible = fighter.GetAvailableUniques()
                    .Where(u => fighter.CanPair(previewed, u)).ToList();
                int previewY   = rowY + CardWidget.WIDGET_H + GAP +
                                 (disabled.Count > 0 ? CardWidget.WIDGET_H + GAP + 14 : 0) + 4;

                sb.DrawString(_smallFont, $"Combos with {AsciiOnly(previewed.Name)}:",
                    new Vector2(20, previewY), Color.DimGray);
                previewY += 13;

                int previewRowW  = compatible.Count * (CardWidget.WIDGET_W + GAP) - GAP;
                int previewStart = Math.Max(20, (screenW - previewRowW) / 2);
                for (int i = 0; i < compatible.Count; i++)
                {
                    var u  = compatible[i];
                    int wx = previewStart + i * (CardWidget.WIDGET_W + GAP);
                    CardWidget.DrawUnique(sb, _pixel, _font, _smallFont, u, fighter, wx, previewY,
                        selected: false, available: true, cooldown: 0,
                        pairContext: new CardPair { Generic = previewed, Unique = u });
                }
            }

            // â”€â”€ Hints â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            sb.DrawString(_smallFont, "[Left/Right] Navigate   [Enter] Select   [Hover] Tooltip",
                new Vector2(20, screenH - 28), Color.DimGray);
        }
        else
        {
            // â”€â”€ STEP 2: Unique/Special card row â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            if (availUni.Count == 0)
            {
                sb.DrawString(_smallFont, "No compatible techniques available.",
                    new Vector2(20, rowY + 20), Color.Red);
                sb.DrawString(_smallFont, "[Backspace] Back",
                    new Vector2(20, rowY + 40), Color.DimGray);
                return;
            }

            for (int i = 0; i < availUni.Count; i++)
            {
                var card = availUni[i];
                bool sel = i == uniIdx;
                int cd   = fighter.GetCooldown(card.Id);
                int wx   = rowStartX + i * (CardWidget.WIDGET_W + GAP);

                var tempPair = new CardPair { Generic = selGen, Unique = card as UniqueCard, Special = card as SpecialCard };
                if (card is UniqueCard uc)
                    CardWidget.DrawUnique(sb, _pixel, _font, _smallFont, uc, fighter, wx, rowY,
                        selected: sel, available: true, cooldown: cd, pairContext: tempPair);
                else if (card is SpecialCard sc)
                    CardWidget.DrawSpecial(sb, _pixel, _font, _smallFont, sc, fighter, wx, rowY,
                        selected: sel, available: true, cooldown: cd);
            }

            // Selected generic recap widget on far left
            int recapX = 20;
            int recapY = rowY;
            if (rowStartX > recapX + CardWidget.WIDGET_W + GAP)
            {
                sb.DrawString(_smallFont, "Using:", new Vector2(recapX, recapY - 14), Color.DimGray);
                CardWidget.DrawGeneric(sb, _pixel, _font, _smallFont, selGen, fighter, recapX, recapY,
                    selected: false, available: true);
            }

            // Unavailable uniques (on cooldown)
            var unavailable = fighter.GetAllUniques()
                .Where(u => !availUni.Contains(u) && fighter.GetCooldown(u.Id) > 0).ToList();
            if (unavailable.Count > 0)
            {
                int dRowY     = rowY + CardWidget.WIDGET_H + GAP + 14;
                int dStartX   = Math.Max(20, (screenW - unavailable.Count * (CardWidget.WIDGET_W + GAP) + GAP) / 2);
                sb.DrawString(_smallFont, "On cooldown:", new Vector2(dStartX, rowY + CardWidget.WIDGET_H + GAP), Color.DimGray);
                for (int i = 0; i < unavailable.Count; i++)
                {
                    var u  = unavailable[i];
                    int wx = dStartX + i * (CardWidget.WIDGET_W + GAP);
                    CardWidget.DrawUnique(sb, _pixel, _font, _smallFont, u, fighter, wx, dRowY,
                        selected: false, available: false, cooldown: fighter.GetCooldown(u.Id),
                        pairContext: new CardPair { Generic = selGen, Unique = u });
                }
            }

            sb.DrawString(_smallFont, "[Left/Right] Navigate   [Enter] Commit   [Backspace] Back",
                new Vector2(20, screenH - 28), Color.DimGray);
        }
    }

    private static string AsciiOnly(string s)
    {
        var b = new System.Text.StringBuilder(s.Length);
        foreach (char c in s) if (c >= 32 && c <= 126) b.Append(c);
        return b.ToString();
    }


    private void DrawResolutionStep(SpriteBatch sb)
    {
        bool showDuring = _match.Phase == MatchPhase.RoundMidpoint ||
                          _match.Phase == MatchPhase.RoundResult ||
                          _match.Phase == MatchPhase.MatchOver;
        if (!showDuring || _resolutionSteps.Count == 0) return;

        try
        {
            int logX = Layout.LeftX, logY = Layout.LogY;
            int displayIdx = Math.Min(_stepIndex, _resolutionSteps.Count - 1);
            var step = _resolutionSteps[displayIdx];

            int totalSteps = _resolutionSteps.Count;
            sb.DrawString(_smallFont, $"Step {displayIdx + 1}/{totalSteps}",
                new Vector2(logX, logY), new Color(100, 100, 160));
            logY += 14;

            int maxLines = Math.Min(step.Count, 10);
            for (int i = 0; i < maxLines; i++)
            {
                var filtered = new System.Text.StringBuilder();
                foreach (char c in step[i])
                {
                    if (c >= 32 && c <= 126) filtered.Append(c);
                    else if (c == '\n' || c == '\t') filtered.Append(' ');
                }
                string safeText = filtered.ToString().Trim();
                if (safeText.Length == 0) { logY += 6; continue; }

                if (safeText.StartsWith("---"))
                {
                    // Phase header -- colored bar + label
                    string label = safeText.Replace("-", "").Trim();
                    Color barColor = label switch
                    {
                        var s when s.Contains("Beginning") => new Color(60,  120, 220),
                        var s when s.Contains("Main")      => new Color(200, 140,  30),
                        var s when s.Contains("Final")     => new Color(180,  50,  50),
                        var s when s.Contains("Start")     => new Color(80,  170,  80),
                        var s when s.Contains("End")       => new Color(100, 100, 100),
                        _                                  => new Color(120, 120, 160),
                    };
                    sb.Draw(_pixel, new Rectangle(logX, logY, 180, 14), barColor * 0.35f);
                    sb.Draw(_pixel, new Rectangle(logX, logY, 3, 14), barColor);
                    sb.DrawString(_smallFont, " " + label.ToUpper(), new Vector2(logX + 4, logY), barColor);
                    logY += 16;
                }
                else if (step[i].StartsWith("  "))
                {
                    sb.DrawString(_smallFont, "  " + safeText, new Vector2(logX + 8, logY), new Color(230, 190, 120));
                    logY += 13;
                }
                else
                {
                    sb.DrawString(_smallFont, safeText, new Vector2(logX, logY), Color.LightGray);
                    logY += 13;
                }
            }

            logY += 4;
            if (_match.Phase == MatchPhase.MatchOver && _resolutionFullyDisplayed)
            {
                // Handled by DrawMatchOver
            }
            else if (_match.Phase == MatchPhase.MatchOver)
            {
                sb.DrawString(_smallFont, "[Enter] Continue...", new Vector2(logX, logY), Color.Yellow);
            }
            else if (_resolutionFullyDisplayed)
            {
                sb.DrawString(_smallFont, "[Enter/Space] Next Round", new Vector2(logX, logY), Color.DimGray);
            }
            else
            {
                bool isMidpoint = _needsSecondHalf && displayIdx == _resolutionSteps.Count - 1;
                string hint = isMidpoint ? "[Enter] Continue (opponent acts)..." : "[Enter] Next";
                sb.DrawString(_smallFont, hint, new Vector2(logX, logY), Color.Yellow);
            }
        }
        catch { /* skip on error */ }
    }

    private void DrawCardReveal(SpriteBatch sb)
    {
        if (_match.Phase != MatchPhase.CardReveal) return;
        if (_match.SelectedPairA == null || _match.SelectedPairB == null) return;

        int screenW = Game.GraphicsDevice.Viewport.Width;
        int screenH = Game.GraphicsDevice.Viewport.Height;
        int cx      = screenW / 2;

        sb.Draw(_pixel, new Rectangle(0, 0, screenW, screenH), Color.Black * 0.78f);

        var pairA = _match.SelectedPairA!;
        var pairB = _match.SelectedPairB!;
        var fa    = _match.FighterA;
        var fb    = _match.FighterB;

        // Compute speeds
        int rawSpeedA = 0;
        if (pairA.Generic != null) rawSpeedA += fa.GetCardSpeed(pairA.Generic);
        if (pairA.Unique  != null) rawSpeedA += fa.GetCardSpeed(pairA.Unique);
        else if (pairA.Special != null) rawSpeedA += fa.GetCardSpeed(pairA.Special);
        int speedA = rawSpeedA + fa.RoundSpeedModifier;

        int rawSpeedB = 0;
        if (pairB.Generic != null) rawSpeedB += fb.GetCardSpeed(pairB.Generic);
        if (pairB.Unique  != null) rawSpeedB += fb.GetCardSpeed(pairB.Unique);
        else if (pairB.Special != null) rawSpeedB += fb.GetCardSpeed(pairB.Special);
        int speedB = rawSpeedB + fb.RoundSpeedModifier;

        // Header
        int topY   = screenH / 4;
        string header = $"ROUND {_match.CurrentRound} - CARDS REVEALED";
        float hw = _font.MeasureString(header).X;
        sb.DrawString(_font, header, new Vector2(cx - hw / 2f, topY), Color.Gold);
        topY += 30;

        // Two card groups side-by-side around center
        const int GAP  = 8;
        int groupW     = CardWidget.WIDGET_W * 2 + GAP;
        int p1Left     = cx - GAP / 2 - groupW;
        int p2Left     = cx + GAP / 2;
        int widgetY    = topY + 24;

        // Fighter name labels
        sb.DrawString(_font, AsciiOnly(fa.DisplayName).ToUpper(), new Vector2(p1Left, topY), Color.CornflowerBlue);
        string vs  = "VS";
        float vsW  = _font.MeasureString(vs).X;
        sb.DrawString(_font, vs, new Vector2(cx - vsW / 2f, topY), new Color(130, 130, 130));
        sb.DrawString(_font, AsciiOnly(fb.DisplayName).ToUpper(), new Vector2(p2Left, topY), Color.Crimson);

        // P1 widgets
        if (pairA.Generic != null)
            CardWidget.DrawGeneric(sb, _pixel, _font, _smallFont, pairA.Generic, fa,
                p1Left, widgetY, selected: false, available: true, cooldown: 0);
        int p1UniX = p1Left + CardWidget.WIDGET_W + GAP;
        if (pairA.Unique != null)
            CardWidget.DrawUnique(sb, _pixel, _font, _smallFont, pairA.Unique, fa,
                p1UniX, widgetY, selected: false, available: true, cooldown: 0, pairContext: pairA);
        else if (pairA.Special != null)
            CardWidget.DrawSpecial(sb, _pixel, _font, _smallFont, pairA.Special, fa,
                p1UniX, widgetY, selected: false, available: true, cooldown: 0);

        // P2 widgets
        if (pairB.Generic != null)
            CardWidget.DrawGeneric(sb, _pixel, _font, _smallFont, pairB.Generic, fb,
                p2Left, widgetY, selected: false, available: true, cooldown: 0);
        int p2UniX = p2Left + CardWidget.WIDGET_W + GAP;
        if (pairB.Unique != null)
            CardWidget.DrawUnique(sb, _pixel, _font, _smallFont, pairB.Unique, fb,
                p2UniX, widgetY, selected: false, available: true, cooldown: 0, pairContext: pairB);
        else if (pairB.Special != null)
            CardWidget.DrawSpecial(sb, _pixel, _font, _smallFont, pairB.Special, fb,
                p2UniX, widgetY, selected: false, available: true, cooldown: 0);

        // Speed order
        int afterY = widgetY + CardWidget.WIDGET_H + 10;
        string orderText;
        Color  orderColor;
        if (speedA > speedB)
        { orderText = $"{AsciiOnly(fa.DisplayName)} acts FIRST  (Spd {speedA:+#;-#;0} vs {speedB:+#;-#;0})"; orderColor = Color.LimeGreen; }
        else if (speedB > speedA)
        { orderText = $"{AsciiOnly(fb.DisplayName)} acts FIRST  (Spd {speedB:+#;-#;0} vs {speedA:+#;-#;0})"; orderColor = Color.OrangeRed; }
        else
        { orderText = $"SIMULTANEOUS  (Spd {speedA:+#;-#;0} tied)"; orderColor = Color.Yellow; }

        float ow = _smallFont.MeasureString(orderText).X;
        sb.DrawString(_smallFont, orderText, new Vector2(cx - ow / 2f, afterY), orderColor);
        afterY += 16;

        // Pre-round log
        if (_preRoundLog.Count > 0)
        {
            for (int i = 0; i < Math.Min(_preRoundLog.Count, 3); i++)
            {
                var filt = new System.Text.StringBuilder();
                foreach (char c in _preRoundLog[i])
                    if (c >= 32 && c <= 126) filt.Append(c);
                float lw = _smallFont.MeasureString(filt.ToString()).X;
                sb.DrawString(_smallFont, filt.ToString(), new Vector2(cx - lw / 2f, afterY + i * 13), new Color(220, 180, 100));
            }
            afterY += Math.Min(_preRoundLog.Count, 3) * 13;
        }

        // Prompt
        string prompt = "[Enter/Space] Begin Round";
        float pw = _smallFont.MeasureString(prompt).X;
        sb.DrawString(_smallFont, prompt, new Vector2(cx - pw / 2f, afterY + 8), Color.Yellow);
    }

    private static string GetRevealAimString(CardPair pair)
    {
        CardBase? card = pair.Unique ?? (CardBase?)pair.Special;
        if (card == null) return "?";
        return card switch
        {
            UniqueCard u  => u.PrimaryTarget == u.SecondaryTarget
                ? u.PrimaryTarget.ToString()
                : $"{u.PrimaryTarget}->{u.SecondaryTarget}",
            SpecialCard s => s.PrimaryTarget == s.SecondaryTarget
                ? s.PrimaryTarget.ToString()
                : $"{s.PrimaryTarget}->{s.SecondaryTarget}",
            _ => "?",
        };
    }

    private void DrawMatchOver(SpriteBatch sb)
    {
        int cx = Game.GraphicsDevice.Viewport.Width / 2;
        int cy = Game.GraphicsDevice.Viewport.Height / 2;
        string result;
        Color col;
        if (_match.IsDraw)
        {
            result = "Draw!";
            col = Color.LightYellow;
        }
        else
        {
            bool won = _match.Winner == _match.FighterA;
            result = won ? "You Win!" : "You Lose!";
            col = won ? Color.Gold : Color.OrangeRed;
        }
        var sz = _font.MeasureString(result);
        sb.DrawString(_font, result, new Vector2(cx - sz.X / 2, cy - 20), col);
        if (_resolutionFullyDisplayed)
        {
            sb.DrawString(_smallFont, "[Enter] / [Esc] Continue",
                new Vector2(cx - _smallFont.MeasureString("[Enter] / [Esc] Continue").X / 2, cy + 20), Color.White);
            sb.DrawString(_smallFont, "[R] Replay",
                new Vector2(cx - _smallFont.MeasureString("[R] Replay").X / 2, cy + 38), new Color(150, 220, 150));
        }
    }

    private void DrawResignConfirm(SpriteBatch sb)
    {
        int cx = Game.GraphicsDevice.Viewport.Width / 2;
        int cy = Game.GraphicsDevice.Viewport.Height / 2;
        sb.Draw(_pixel, new Rectangle(cx - 210, cy - 55, 420, 110), Color.Black * 0.75f);
        string title = _isLocalPvP ? "Player 1 - Resign?" : "Resign?";
        string hint  = "[Y] Yes, resign   [N] / [Esc] Cancel";
        sb.DrawString(_font,      title, new Vector2(cx - _font.MeasureString(title).X / 2,      cy - 40), Color.OrangeRed);
        sb.DrawString(_smallFont, hint,  new Vector2(cx - _smallFont.MeasureString(hint).X / 2,  cy + 5),  Color.White);
    }

    // Simple filled hex approximation using filled rectangle + rotated quads is complex in MB,
    // so we draw a filled square as placeholder for now
    private void DrawHex(SpriteBatch sb, int cx, int cy, int size, Color color)
    {
        // Pointy-top hexagon filled via horizontal scanlines.
        // 6 vertices at 30Â°, 90Â°, 150Â°, 210Â°, 270Â°, 330Â° from center.
        // For each scanline row, compute left/right X edges from the hex boundary.
        float r = size;
        float h = r;          // half-height = r (pointy-top: top vertex at cy - r)
        float w = r * 0.866f; // half-width  = r * sin(60Â°)

        // The hex has 3 zones (top-to-bottom for pointy-top):
        //   Zone A: cy-r  to  cy-r/2   â€” top triangle (narrows as y increases, width = 2w*(y-top)/(r/2))
        //   Zone B: cy-r/2 to cy+r/2  â€” rectangular band (full width = 2w)
        //   Zone C: cy+r/2 to cy+r    â€” bottom triangle (narrows, width = 2w*(bottom-y)/(r/2))

        float alpha = color.A / 255f * 0.85f;
        Color c = color * alpha;

        int yTop    = (int)(cy - h);
        int yMidTop = (int)(cy - h / 2f);
        int yMidBot = (int)(cy + h / 2f);
        int yBot    = (int)(cy + h);

        for (int row = yTop; row <= yBot; row++)
        {
            float halfW;
            if (row <= yMidTop) // top triangle zone
            {
                float t = (float)(row - yTop) / (yMidTop - yTop + 1);
                halfW = w * t;
            }
            else if (row <= yMidBot) // middle band
            {
                halfW = w;
            }
            else // bottom triangle zone
            {
                float t = (float)(yBot - row) / (yBot - yMidBot + 1);
                halfW = w * t;
            }

            int x0 = (int)(cx - halfW);
            int x1 = (int)(cx + halfW);
            int width = Math.Max(1, x1 - x0);
            sb.Draw(_pixel, new Rectangle(x0, row, width, 1), c);
        }
    }

    /// <summary>Returns a compact keyword string for inline card display, e.g. " [Stagger,BLD]" or empty.</summary>
    private static string GetKeywordDisplay(Models.Cards.CardBase card)
    {
        if (card.Keywords.Count == 0) return "";
        var parts = card.Keywords.Select(kw => kw.Keyword switch
        {
            Models.Cards.CardKeyword.MaxDamageCap  => kw.Value == 1 ? "Cap:Brs" : kw.Value == 2 ? "Cap:Inj" : "Cap",
            Models.Cards.CardKeyword.CurseGain     => "+Token",
            Models.Cards.CardKeyword.CursePull     => "CursePull",
            Models.Cards.CardKeyword.CurseEmpower  => "CurseEmp",
            Models.Cards.CardKeyword.CurseWeaken   => "CurseWkn",
            Models.Cards.CardKeyword.Bleed         => "Bleed",
            Models.Cards.CardKeyword.ArmorBreak    => "ArmBrk",
            Models.Cards.CardKeyword.Piercing      => "Pierce",
            Models.Cards.CardKeyword.Crushing      => "Crush",
            Models.Cards.CardKeyword.Stagger       => "Stagger",
            Models.Cards.CardKeyword.Knockback     => "KB",
            Models.Cards.CardKeyword.Lunge         => "Lunge",
            Models.Cards.CardKeyword.Guard         => "Guard",
            Models.Cards.CardKeyword.Parry         => "Parry",
            Models.Cards.CardKeyword.Pull          => "Pull",
            Models.Cards.CardKeyword.ChivalryBonus => $"ChivBonus+{kw.Value}",
            Models.Cards.CardKeyword.DistanceGuard => $"DistGuard({kw.Value}+)",
            _                                      => kw.Keyword.ToString(),
        });
        return " [" + string.Join(",", parts) + "]";
    }

    private void DrawCardTooltip(SpriteBatch sb, MouseState mouse)
    {
        if (_hoveredCard == null) return;

        var tooltipLines = CardTooltip.GetCardTooltip(_hoveredCard);
        const int padding = 8;
        const int lineHeight = 14;
        int maxWidth = tooltipLines.Max(line => line.Length) * 8;  // ~8 pixels per char
        int tooltipWidth = maxWidth + padding * 2;
        int tooltipHeight = tooltipLines.Count * lineHeight + padding * 2;

        // Position tooltip near mouse, but keep it on screen
        int tooltipX = mouse.X + 15;
        int tooltipY = mouse.Y + 15;

        int screenWidth = Game.GraphicsDevice.Viewport.Width;
        int screenHeight = Game.GraphicsDevice.Viewport.Height;

        if (tooltipX + tooltipWidth > screenWidth)
            tooltipX = screenWidth - tooltipWidth - 5;
        if (tooltipY + tooltipHeight > screenHeight)
            tooltipY = screenHeight - tooltipHeight - 5;

        // Draw background
        sb.Draw(_pixel, new Rectangle(tooltipX, tooltipY, tooltipWidth, tooltipHeight), Color.Black * 0.85f);
        // Draw border
        DrawRect(sb, tooltipX - 1, tooltipY - 1, tooltipWidth + 2, tooltipHeight + 2, Color.Gold);

        // Draw text
        int textY = tooltipY + padding;
        foreach (var line in tooltipLines)
        {
            Color lineColor = Color.White;
            // Highlight keywords
            if (line.Contains(":")
                && (line.Contains("Bleed") || line.Contains("Break") || line.Contains("Piercing")
                    || line.Contains("Crushing") || line.Contains("Feint") || line.Contains("Quickstep")
                    || line.Contains("Lunge") || line.Contains("Stagger") || line.Contains("Disrupt")
                    || line.Contains("Knockback") || line.Contains("Guard") || line.Contains("Parry")
                    || line.Contains("Deflect") || line.Contains("Sidestep") || line.Contains("Press")
                    || line.Contains("Retreat") || line.Contains("Kill")
                    || line.Contains("Curse") || line.Contains("MaxDamage") || line.Contains("Cap:")
                    || line.Contains("Pull") || line.Contains("Chivalry") || line.Contains("Distance")))
            {
                lineColor = Color.LimeGreen;
            }

            sb.DrawString(_smallFont, line, new Vector2(tooltipX + padding, textY), lineColor);
            textY += lineHeight;
        }
    }

    private void DrawRect(SpriteBatch sb, int x, int y, int width, int height, Color color)
    {
        // Draw border using pixel lines
        sb.Draw(_pixel, new Rectangle(x, y, width, 1), color);          // Top
        sb.Draw(_pixel, new Rectangle(x, y + height - 1, width, 1), color);  // Bottom
        sb.Draw(_pixel, new Rectangle(x, y, 1, height), color);         // Left
        sb.Draw(_pixel, new Rectangle(x + width - 1, y, 1, height), color);  // Right
    }
}
