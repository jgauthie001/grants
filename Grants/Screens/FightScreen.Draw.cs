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

        // Draw stage hazardous hexes (tokens, etc.) — orange/gold, rendered under fighters
        var hazardHexes = _match.Stage.GetHazardousHexes(_match.StageState);
        foreach (var hh in hazardHexes)
        {
            if (_match.Board.IsValid(hh))
            {
                var (hpx, hpy) = Models.Board.HexBoard.HexToPixel(hh, L.HexSize, L.BoardCenterX, L.BoardCenterY);
                DrawHex(sb, (int)hpx, (int)hpy, (int)L.HexSize - 6, new Color(200, 140, 40));
            }
        }

        // Draw persona board overlays (e.g. Revenant Witch spirits) — rendered under fighters
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
        int x = Layout.RightX, y = 200;

        sb.DrawString(_font, "Opponent Cards", new Vector2(x, y), Color.LightGray);
        y += 24;

        // Generic cards — color by body location damage state
        sb.DrawString(_smallFont, "Body:", new Vector2(x, y), Color.DimGray);
        y += 16;
        foreach (var card in opp.Definition.GenericCards)
        {
            var loc = Models.Fighter.FighterInstance.BodyPartToLocation(card.BodyPart);
            var state = opp.LocationStates[loc].State;
            int cd = opp.GetCooldown(card.Id);
            Color c = state switch
            {
                Models.Fighter.DamageState.Disabled => Color.Red,
                Models.Fighter.DamageState.Injured  => Color.Orange,
                Models.Fighter.DamageState.Bruised  => Color.Yellow,
                _                                   => cd > 0 ? new Color(100, 100, 100) : Color.LightGray,
            };
            string cdStr = cd > 0 ? $" [CD:{cd}]" : "";
            string stateStr = state != Models.Fighter.DamageState.Healthy ? $" [{state}]" : "";
            sb.DrawString(_smallFont, $"  {card.Name}{stateStr}{cdStr}", new Vector2(x, y), c);
            y += 16;
        }

        y += 6;

        // Unique cards — color by cooldown
        sb.DrawString(_smallFont, "Moves:", new Vector2(x, y), Color.DimGray);
        y += 16;
        foreach (var card in opp.GetAllUniques())
        {
            int cd = opp.GetCooldown(card.Id);
            Color c = cd > 0 ? new Color(100, 100, 100) : Color.LightGray;
            string cdStr = cd > 0 ? $" [CD:{cd}]" : "";
            sb.DrawString(_smallFont, $"  {card.Name}{cdStr}", new Vector2(x, y), c);
            y += 16;
        }

        // Special cards
        foreach (var card in opp.Definition.SpecialCards)
        {
            int cd = opp.GetCooldown(card.Id);
            Color c = cd > 0 ? new Color(100, 100, 100) : Color.LightGray;
            string cdStr = cd > 0 ? $" [CD:{cd}]" : "";
            sb.DrawString(_smallFont, $"  {card.Name}{cdStr}", new Vector2(x, y), c);
            y += 16;
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

        int panelX = 20, panelY = 200;
        int cx = Game.GraphicsDevice.Viewport.Width / 2;
        int cy = Game.GraphicsDevice.Viewport.Height / 2;

        // --- Pass-to-P2 overlay ---
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

        // --- P2 card selection ---
        if (_pvpPhase == LocalPvpPhase.P2Selecting)
        {
            sb.DrawString(_font, "Player 2 - Choose your cards", new Vector2(panelX, panelY - 30), Color.Cyan);

            if (_p2SelectedGeneric == null)
            {
                sb.DrawString(_font, "Select Generic Card:", new Vector2(panelX, panelY), Color.White);
                for (int i = 0; i < _p2ValidGenerics.Count; i++)
                {
                    var card = _p2ValidGenerics[i];
                    bool sel = i == _p2GenericIndex;
                    Color c = sel ? Color.Yellow : Color.LightGray;
                    string mvTypeG = card.BaseMovementType switch
                    {
                        Models.Cards.MovementType.Approach => ">",
                        Models.Cards.MovementType.Retreat  => "<",
                        Models.Cards.MovementType.Free     => "*",
                        _                                  => "-",
                    };
                    string mvStrG = card.MaxMovement == 0 ? "-" :
                        card.MinMovement == card.MaxMovement ? $"{mvTypeG}{card.MaxMovement}" :
                        $"{mvTypeG}{card.MinMovement}-{card.MaxMovement}";
                    string label = $"{(sel ? ">" : " ")} {card.Name}  [Spd:{card.BaseSpeed:+#;-#;0} Pwr:{card.BasePower} Def:{card.BaseDefense} Mv:{mvStrG}]" + GetKeywordDisplay(card);
                    sb.DrawString(_smallFont, label, new Vector2(panelX, panelY + 24 + i * 18), c);
                }
                sb.DrawString(_smallFont, "[Up/Down] Navigate   [Enter] Select",
                    new Vector2(panelX, panelY + 24 + _p2ValidGenerics.Count * 18 + 8), Color.DimGray);
            }
            else
            {
                var p2Defender = _match.FighterA;
                sb.DrawString(_font, $"Select Combo for: {_p2SelectedGeneric.Name}", new Vector2(panelX, panelY), Color.Yellow);
                if (_p2ValidUniques.Count == 0)
                {
                    sb.DrawString(_smallFont, "No compatible moves available", new Vector2(panelX, panelY + 30), Color.Red);
                    sb.DrawString(_smallFont, "[Backspace] Go back", new Vector2(panelX, panelY + 50), Color.DimGray);
                    return;
                }
                for (int i = 0; i < _p2ValidUniques.Count; i++)
                {
                    var card = _p2ValidUniques[i];
                    bool sel = i == _p2UniqueIndex;
                    Color c = sel ? Color.Yellow : Color.LightGray;
                    string cardName = card switch { UniqueCard u => u.Name, SpecialCard s => s.Name, _ => "?" };
                    var tempPair = new CardPair { Generic = _p2SelectedGeneric, Unique = card as UniqueCard, Special = card as SpecialCard };
                    string rangeStr = $"{tempPair.EffectiveMinRange}-{tempPair.EffectiveMaxRange}";
                    string targetStr = card switch
                    {
                        UniqueCard u => u.PrimaryTarget == u.SecondaryTarget ? $"{u.PrimaryTarget}" :
                            p2Defender.LocationStates[u.PrimaryTarget].State == Models.Fighter.DamageState.Disabled
                                ? $"[{u.PrimaryTarget}]->{u.SecondaryTarget}" : $"{u.PrimaryTarget}->{u.SecondaryTarget}",
                        SpecialCard s => s.PrimaryTarget == s.SecondaryTarget ? $"{s.PrimaryTarget}" :
                            p2Defender.LocationStates[s.PrimaryTarget].State == Models.Fighter.DamageState.Disabled
                                ? $"[{s.PrimaryTarget}]->{s.SecondaryTarget}" : $"{s.PrimaryTarget}->{s.SecondaryTarget}",
                        _ => "?",
                    };
                    Color targetColor = card is UniqueCard uu &&
                        p2Defender.LocationStates[uu.PrimaryTarget].State == Models.Fighter.DamageState.Disabled
                        ? Color.Orange : Color.LightCyan;
                    string label = $"{(sel ? ">" : " ")} {cardName}  [Spd:{card.BaseSpeed:+#;-#;0} Pwr:{card.BasePower} Def:{card.BaseDefense} Rng:{rangeStr}]" + GetKeywordDisplay(card);
                    sb.DrawString(_smallFont, label, new Vector2(panelX, panelY + 24 + i * 18), c);
                    var labelSize = _smallFont.MeasureString(label);
                    sb.DrawString(_smallFont, $" Aim:{targetStr}", new Vector2(panelX + labelSize.X, panelY + 24 + i * 18), targetColor);
                }
                sb.DrawString(_smallFont, "[Up/Down] Navigate   [Enter] Commit   [Backspace] Back",
                    new Vector2(panelX, panelY + 24 + _p2ValidUniques.Count * 18 + 8), Color.DimGray);
            }
            return;
        }

        // --- Normal P1 card selection ---
        if (_playerCommitted) return;

        if (_selectedGeneric == null)
        {
            // Step 1: Select Generic Card
            sb.DrawString(_font, "Select Generic Card:_pl", new Vector2(panelX, panelY), Color.White);

            for (int i = 0; i < _validGenerics.Count; i++)
            {
                var card = _validGenerics[i];
                bool sel = i == _genericSelectionIndex;
                Color c = sel ? Color.Yellow : Color.LightGray;
                string mvTypeG = card.BaseMovementType switch
                {
                    Models.Cards.MovementType.Approach => ">",
                    Models.Cards.MovementType.Retreat  => "<",
                    Models.Cards.MovementType.Free     => "*",
                    _                                  => "-",
                };
                string mvStrG = card.MaxMovement == 0 ? "-" :
                    card.MinMovement == card.MaxMovement ? $"{mvTypeG}{card.MaxMovement}" :
                    $"{mvTypeG}{card.MinMovement}-{card.MaxMovement}";
                int comboCount = _match.FighterA.GetAvailableUniques().Count(u => _match.FighterA.CanPair(card, u));
                string comboTag = $" ({comboCount})";
                string label = $"{(sel ? ">" : " ")} {card.Name}  [Spd:{card.BaseSpeed:+#;-#;0} Pwr:{card.BasePower} Def:{card.BaseDefense} Mv:{mvStrG}]{comboTag}" + GetKeywordDisplay(card);
                sb.DrawString(_smallFont, label, new Vector2(panelX, panelY + 24 + i * 18), c);
            }

            // Preview compatible uniques for the highlighted generic
            int afterPreviewY = panelY + 24 + _validGenerics.Count * 18 + 10;
            if (_validGenerics.Count > 0)
            {
                var previewGeneric = _validGenerics[_genericSelectionIndex];
                var previewAvail = _match.FighterA.GetAvailableUniques()
                    .Where(u => _match.FighterA.CanPair(previewGeneric, u)).ToList();
                var previewCd = _match.FighterA.GetAllUniques()
                    .Where(u => _match.FighterA.CanPair(previewGeneric, u) && _match.FighterA.GetCooldown(u.Id) > 0).ToList();
                var previewIncompat = _match.FighterA.GetAllUniques()
                    .Where(u => !_match.FighterA.CanPair(previewGeneric, u)).ToList();

                int previewY = afterPreviewY;
                sb.DrawString(_smallFont, $"Combos with {previewGeneric.Name}:", new Vector2(panelX, previewY), Color.DimGray);
                previewY += 14;
                foreach (var u in previewAvail)
                { sb.DrawString(_smallFont, $"  + {u.Name}", new Vector2(panelX, previewY), new Color(100, 200, 120)); previewY += 14; }
                foreach (var u in previewCd)
                { sb.DrawString(_smallFont, $"  ~ {u.Name} [CD:{_match.FighterA.GetCooldown(u.Id)}]", new Vector2(panelX, previewY), new Color(130, 130, 130)); previewY += 14; }
                foreach (var u in previewIncompat)
                { sb.DrawString(_smallFont, $"  x {u.Name}", new Vector2(panelX, previewY), new Color(80, 80, 80)); previewY += 14; }

                sb.DrawString(_smallFont, "[Up/Down] Navigate   [Enter] Select",
                    new Vector2(panelX, previewY + 6), Color.DimGray);
                afterPreviewY = previewY + 20;
            }

            // Show disabled/on-cooldown generics so player can see what they've lost
            var disabledGenerics = _match.FighterA.Definition.GenericCards
                .Where(c => !_validGenerics.Contains(c))
                .ToList();
            if (disabledGenerics.Count > 0)
            {
                int offsetY = afterPreviewY + 10;
                sb.DrawString(_smallFont, "Unavailable:", new Vector2(panelX, offsetY), Color.DimGray);
                offsetY += 16;
                foreach (var card in disabledGenerics)
                {
                    var loc = Models.Fighter.FighterInstance.BodyPartToLocation(card.BodyPart);
                    bool isDisabled = _match.FighterA.LocationStates[loc].State == Models.Fighter.DamageState.Disabled;
                    int cd = _match.FighterA.GetCooldown(card.Id);
                    string reason = isDisabled ? "[DISABLED]" : $"[CD:{cd}]";
                    Color dimColor = isDisabled ? new Color(180, 40, 40) : new Color(100, 100, 100);
                    sb.DrawString(_smallFont, $"  {card.Name} {reason}", new Vector2(panelX, offsetY), dimColor);
                    offsetY += 16;
                }
            }
        }
        else
        {
            // Step 2: Select Unique/Special Card
            sb.DrawString(_font, $"Select Combo for: {_selectedGeneric.Name}", new Vector2(panelX, panelY), Color.Yellow);

            if (_validUniques.Count == 0)
            {
                sb.DrawString(_smallFont, "No compatible moves available", new Vector2(panelX, panelY + 30), Color.Red);
                sb.DrawString(_smallFont, "[Esc] Go back", new Vector2(panelX, panelY + 50), Color.DimGray);
                return;
            }

            var defender = _match.FighterB;
            for (int i = 0; i < _validUniques.Count; i++)
            {
                var card = _validUniques[i];
                bool sel = i == _uniqueSelectionIndex;
                Color c = sel ? Color.Yellow : Color.LightGray;
                string cardName = card switch
                {
                    UniqueCard u => u.Name,
                    SpecialCard s => s.Name,
                    _ => "?"
                };
                // Compute effective range and combined movement for this pair
                var tempPair = new CardPair { Generic = _selectedGeneric, Unique = card as UniqueCard, Special = card as SpecialCard };
                string rangeStr = $"{tempPair.EffectiveMinRange}-{tempPair.EffectiveMaxRange}";
                int combinedMinMv = tempPair.EffectiveMinMovement;
                int combinedMaxMv = _match.FighterA.GetCardMovement(_selectedGeneric!)
                    + _match.FighterA.GetCardMovement(card);
                string mvType = tempPair.CombinedMovementType switch
                {
                    Models.Cards.MovementType.Approach => ">",
                    Models.Cards.MovementType.Retreat  => "<",
                    Models.Cards.MovementType.Free     => "*",
                    _                                  => "-",
                };
                string mvStr = combinedMaxMv == 0 ? "-" :
                    combinedMinMv == combinedMaxMv ? $"{mvType}{combinedMaxMv}" :
                    $"{mvType}{combinedMinMv}-{combinedMaxMv}";

                // Build target indicator: "Head->Torso" or just "Head" if both are same
                string targetStr = card switch
                {
                    UniqueCard u => u.PrimaryTarget == u.SecondaryTarget
                        ? $"{u.PrimaryTarget}"
                        : defender.LocationStates[u.PrimaryTarget].State == Models.Fighter.DamageState.Disabled
                            ? $"[{u.PrimaryTarget}]->{u.SecondaryTarget}"
                            : $"{u.PrimaryTarget}->{u.SecondaryTarget}",
                    SpecialCard s => s.PrimaryTarget == s.SecondaryTarget
                        ? $"{s.PrimaryTarget}"
                        : defender.LocationStates[s.PrimaryTarget].State == Models.Fighter.DamageState.Disabled
                            ? $"[{s.PrimaryTarget}]->{s.SecondaryTarget}"
                            : $"{s.PrimaryTarget}->{s.SecondaryTarget}",
                    _ => "?",
                };
                Color targetColor = card switch
                {
                    UniqueCard u when defender.LocationStates[u.PrimaryTarget].State == Models.Fighter.DamageState.Disabled
                        => Color.Orange,
                    _ => Color.LightCyan,
                };

                string label = $"{(sel ? ">" : " ")} {cardName}  [Spd:{card.BaseSpeed:+#;-#;0} Pwr:{card.BasePower} Def:{card.BaseDefense} Mv:{mvStr} Rng:{rangeStr}]" + GetKeywordDisplay(card);
                sb.DrawString(_smallFont, label, new Vector2(panelX, panelY + 24 + i * 18), c);
                // Draw target indicator to the right of the main label
                var labelSize = _smallFont.MeasureString(label);
                sb.DrawString(_smallFont, $" Aim:{targetStr}", new Vector2(panelX + labelSize.X, panelY + 24 + i * 18), targetColor);
            }

            sb.DrawString(_smallFont, "[Up/Down] Navigate   [Enter] Commit   [Backspace] Back",
                new Vector2(panelX, panelY + 24 + _validUniques.Count * 18 + 8), Color.DimGray);

            // Show on-cooldown uniques and incompatible uniques
            var unavailableUniques = _match.FighterA.GetAllUniques()
                .Where(u => !_validUniques.Contains(u) && _match.FighterA.GetCooldown(u.Id) > 0)
                .ToList();
            var incompatibleUniques = _match.FighterA.GetAllUniques()
                .Where(u => !_match.FighterA.CanPair(_selectedGeneric!, u))
                .ToList();
            int trailingY = panelY + 24 + _validUniques.Count * 18 + 30;
            foreach (var u in unavailableUniques)
            {
                int cd = _match.FighterA.GetCooldown(u.Id);
                sb.DrawString(_smallFont, $"  ~ {u.Name} [CD:{cd}]",
                    new Vector2(panelX, trailingY), new Color(130, 130, 130));
                trailingY += 16;
            }
            foreach (var u in incompatibleUniques)
            {
                string tags = u.RequiredBodyTags.Count > 0 ? string.Join("/", u.RequiredBodyTags) : "any";
                sb.DrawString(_smallFont, $"  x {u.Name} [needs: {tags}]",
                    new Vector2(panelX, trailingY), new Color(80, 80, 80));
                trailingY += 16;
            }
        }
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
            string stepLabel = $"Round action {displayIdx + 1}/{totalSteps}:";
            sb.DrawString(_smallFont, stepLabel, new Vector2(logX, logY), new Color(150, 150, 200));
            logY += 14;

            int maxLines = Math.Min(step.Count, 7);
            for (int i = 0; i < maxLines; i++)
            {
                var filtered = new System.Text.StringBuilder();
                foreach (char c in step[i])
                {
                    if (c >= 32 && c <= 126) filtered.Append(c);
                    else if (c == '\n' || c == '\t') filtered.Append(' ');
                }
                string safeText = filtered.ToString();
                // Indent lines (start with spaces) are keyword/detail lines — highlight differently
                Color lineColor = step[i].StartsWith("  ") ? new Color(230, 190, 120) : Color.LightGray;
                sb.DrawString(_smallFont, safeText, new Vector2(logX, logY + i * 14), lineColor);
            }

            int promptY = logY + maxLines * 14 + 4;

            if (_match.Phase == MatchPhase.MatchOver && _resolutionFullyDisplayed)
            {
                // Handled by DrawMatchOver
            }
            else if (_match.Phase == MatchPhase.MatchOver)
            {
                sb.DrawString(_smallFont, "[Enter] Continue...", new Vector2(logX, promptY), Color.Yellow);
            }
            else if (_resolutionFullyDisplayed)
            {
                sb.DrawString(_smallFont, "[Enter/Space] Next Round", new Vector2(logX, promptY), Color.DimGray);
            }
            else
            {
                bool isMidpoint = _needsSecondHalf && displayIdx == _resolutionSteps.Count - 1;
                string hint = isMidpoint ? "[Enter] Continue (opponent acts)..." : "[Enter] Next";
                sb.DrawString(_smallFont, hint, new Vector2(logX, promptY), Color.Yellow);
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

        // Dark overlay panel covering lower portion of screen
        sb.Draw(_pixel, new Rectangle(0, screenH / 2 - 40, screenW, screenH / 2 + 40), Color.Black * 0.88f);

        var pairA = _match.SelectedPairA!;
        var pairB = _match.SelectedPairB!;
        var fa = _match.FighterA;
        var fb = _match.FighterB;

        // Compute combined stats (mirrors ResolutionEngine logic)
        int rawSpeedA = 0;
        if (pairA.Generic != null) rawSpeedA += fa.GetCardSpeed(pairA.Generic);
        if (pairA.Unique != null) rawSpeedA += fa.GetCardSpeed(pairA.Unique);
        else if (pairA.Special != null) rawSpeedA += fa.GetCardSpeed(pairA.Special);
        int speedA = rawSpeedA + fa.RoundSpeedModifier;

        int rawSpeedB = 0;
        if (pairB.Generic != null) rawSpeedB += fb.GetCardSpeed(pairB.Generic);
        if (pairB.Unique != null) rawSpeedB += fb.GetCardSpeed(pairB.Unique);
        else if (pairB.Special != null) rawSpeedB += fb.GetCardSpeed(pairB.Special);
        int speedB = rawSpeedB + fb.RoundSpeedModifier;

        int powerA = (pairA.Generic?.BasePower ?? 0) + (pairA.Unique?.BasePower ?? pairA.Special?.BasePower ?? 0) + fa.RoundPowerModifier;
        int defA   = (pairA.Generic?.BaseDefense ?? 0) + (pairA.Unique?.BaseDefense ?? pairA.Special?.BaseDefense ?? 0);
        int powerB = (pairB.Generic?.BasePower ?? 0) + (pairB.Unique?.BasePower ?? pairB.Special?.BasePower ?? 0) + fb.RoundPowerModifier;
        int defB   = (pairB.Generic?.BaseDefense ?? 0) + (pairB.Unique?.BaseDefense ?? pairB.Special?.BaseDefense ?? 0);

        int panelY = screenH / 2 - 30;
        int leftX  = 80;
        int rightX = screenW - 420;
        int cx     = screenW / 2;

        // Header
        string header = $"ROUND {_match.CurrentRound} - CARDS REVEALED";
        float hw = _font.MeasureString(header).X;
        sb.DrawString(_font, header, new Vector2(cx - hw / 2f, panelY), Color.Gold);
        panelY += 32;

        // Fighter names
        sb.DrawString(_font, fa.DisplayName.ToUpper(), new Vector2(leftX, panelY), Color.CornflowerBlue);
        string vsText = "VS";
        float vsW = _font.MeasureString(vsText).X;
        sb.DrawString(_font, vsText, new Vector2(cx - vsW / 2f, panelY), new Color(130, 130, 130));
        sb.DrawString(_font, fb.DisplayName.ToUpper(), new Vector2(rightX, panelY), Color.Crimson);
        panelY += 26;

        // Generic card name (+ keywords)
        string genNameA = pairA.Generic?.Name ?? pairA.Special?.Name ?? "?";
        string genNameB = pairB.Generic?.Name ?? pairB.Special?.Name ?? "?";
        string genKwA = pairA.Generic != null ? GetKeywordDisplay(pairA.Generic) : "";
        string genKwB = pairB.Generic != null ? GetKeywordDisplay(pairB.Generic) : "";
        sb.DrawString(_smallFont, genNameA + genKwA, new Vector2(leftX, panelY), Color.White);
        sb.DrawString(_smallFont, genNameB + genKwB, new Vector2(rightX, panelY), Color.White);
        panelY += 16;

        // Unique/special card name (+ keywords)
        if (pairA.Unique != null)
        {
            string uniA = $"+ {pairA.Unique.Name}" + GetKeywordDisplay(pairA.Unique);
            sb.DrawString(_smallFont, uniA, new Vector2(leftX, panelY), Color.LightGray);
        }
        else if (pairA.Special != null && pairA.Generic == null)
        {
            sb.DrawString(_smallFont, $"+ {pairA.Special.Name}" + GetKeywordDisplay(pairA.Special), new Vector2(leftX, panelY), Color.LightGray);
        }
        if (pairB.Unique != null)
        {
            string uniB = $"+ {pairB.Unique.Name}" + GetKeywordDisplay(pairB.Unique);
            sb.DrawString(_smallFont, uniB, new Vector2(rightX, panelY), Color.LightGray);
        }
        else if (pairB.Special != null && pairB.Generic == null)
        {
            sb.DrawString(_smallFont, $"+ {pairB.Special.Name}" + GetKeywordDisplay(pairB.Special), new Vector2(rightX, panelY), Color.LightGray);
        }
        panelY += 16;

        // Combined stats
        Color spdColorA = speedA > speedB ? Color.LimeGreen : speedA < speedB ? Color.OrangeRed : Color.White;
        Color spdColorB = speedB > speedA ? Color.LimeGreen : speedB < speedA ? Color.OrangeRed : Color.White;
        string statsA = $"Spd:{speedA:+#;-#;0}  Pwr:{powerA}  Def:{defA}";
        string statsB = $"Spd:{speedB:+#;-#;0}  Pwr:{powerB}  Def:{defB}";
        sb.DrawString(_smallFont, statsA, new Vector2(leftX, panelY), spdColorA);
        sb.DrawString(_smallFont, statsB, new Vector2(rightX, panelY), spdColorB);
        panelY += 16;

        // Range and aim
        string rangeA = $"Rng:{pairA.EffectiveMinRange}-{pairA.EffectiveMaxRange}";
        string rangeB = $"Rng:{pairB.EffectiveMinRange}-{pairB.EffectiveMaxRange}";
        string aimA   = GetRevealAimString(pairA);
        string aimB   = GetRevealAimString(pairB);
        sb.DrawString(_smallFont, $"{rangeA}  Aim:{aimA}", new Vector2(leftX, panelY), Color.LightCyan);
        sb.DrawString(_smallFont, $"{rangeB}  Aim:{aimB}", new Vector2(rightX, panelY), Color.LightCyan);
        panelY += 20;

        // Speed order
        string orderText;
        Color orderColor;
        if (speedA > speedB)
        {
            orderText  = $"{fa.DisplayName} acts FIRST";
            orderColor = Color.LimeGreen;
        }
        else if (speedB > speedA)
        {
            orderText  = $"{fb.DisplayName} acts FIRST";
            orderColor = Color.OrangeRed;
        }
        else
        {
            orderText  = "SIMULTANEOUS (tied speed)";
            orderColor = Color.Yellow;
        }
        float ow = _smallFont.MeasureString(orderText).X;
        sb.DrawString(_smallFont, orderText, new Vector2(cx - ow / 2f, panelY), orderColor);
        panelY += 18;

        // Pre-round log (persona/stage choices that happened this round)
        if (_preRoundLog.Count > 0)
        {
            for (int i = 0; i < Math.Min(_preRoundLog.Count, 3); i++)
            {
                var filt = new System.Text.StringBuilder();
                foreach (char c in _preRoundLog[i])
                    if (c >= 32 && c <= 126) filt.Append(c);
                float lw = _smallFont.MeasureString(filt.ToString()).X;
                sb.DrawString(_smallFont, filt.ToString(), new Vector2(cx - lw / 2f, panelY + i * 13), new Color(220, 180, 100));
            }
            panelY += Math.Min(_preRoundLog.Count, 3) * 13;
        }

        // Prompt
        string prompt = "[Enter/Space] Begin Round";
        float pw = _smallFont.MeasureString(prompt).X;
        sb.DrawString(_smallFont, prompt, new Vector2(cx - pw / 2f, panelY), Color.Yellow);
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
        // 6 vertices at 30°, 90°, 150°, 210°, 270°, 330° from center.
        // For each scanline row, compute left/right X edges from the hex boundary.
        float r = size;
        float h = r;          // half-height = r (pointy-top: top vertex at cy - r)
        float w = r * 0.866f; // half-width  = r * sin(60°)

        // The hex has 3 zones (top-to-bottom for pointy-top):
        //   Zone A: cy-r  to  cy-r/2   — top triangle (narrows as y increases, width = 2w*(y-top)/(r/2))
        //   Zone B: cy-r/2 to cy+r/2  — rectangular band (full width = 2w)
        //   Zone C: cy+r/2 to cy+r    — bottom triangle (narrows, width = 2w*(bottom-y)/(r/2))

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
