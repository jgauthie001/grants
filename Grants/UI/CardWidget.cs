using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Grants.Models.Cards;
using Grants.Models.Fighter;

namespace Grants.UI;

/// <summary>
/// Renders a single card as a framed box with:
///   - Art placeholder (solid colour block, ready to swap for a Texture2D later)
///   - Card name
///   - Phase badge (Beginning / Main / Final coloured pill)
///   - Key stats: Speed, Power, Defense, Movement, Range (whichever apply)
///   - Keywords (single condensed line)
///
/// All coordinates are top-left of the widget.  The widget is always WIDGET_W × WIDGET_H pixels.
/// Callers should not hard-code those values — use the constants.
/// </summary>
public static class CardWidget
{
    public const int WIDGET_W  = 170;
    public const int WIDGET_H  = 110;
    public const int ART_H     = 36;   // art placeholder block height (px)
    public const int COMPACT_H = 60;   // compact variant: no art block

    private const int PADDING  = 4;
    private const int LINE_H   = 13;

    // Phase badge colours
    private static readonly Color ColBeginning = new Color(60,  120, 220);   // blue
    private static readonly Color ColMain      = new Color(200, 140,  30);   // gold
    private static readonly Color ColFinal     = new Color(180,  50,  50);   // red
    private static readonly Color ColStart     = new Color(80,  170,  80);   // green
    private static readonly Color ColEnd       = new Color(100, 100, 100);   // grey

    /// <summary>
    /// Draw a generic card widget.
    /// </summary>
    public static void DrawGeneric(
        SpriteBatch sb, Texture2D pixel, SpriteFont font, SpriteFont small,
        GenericCard card, FighterInstance owner,
        int x, int y,
        bool selected, bool available,
        int cooldown = 0)
    {
        Color border   = selected  ? Color.Yellow :
                         !available ? new Color(60, 60, 60) : new Color(80, 100, 130);
        Color nameTint = !available ? Color.DimGray : Color.White;

        DrawFrame(sb, pixel, x, y, border);
        DrawArtPlaceholder(sb, pixel, x, y, new Color(30, 50, 80));

        int tx = x + PADDING;
        int ty = y + ART_H + PADDING;

        // Name row
        string name = AsciiFilter(card.Name);
        if (cooldown > 0) name += $" [{cooldown}]";
        DrawTruncated(sb, small, name, tx, ty, WIDGET_W - PADDING * 2, nameTint);
        ty += LINE_H;

        // Phase badge for movement
        DrawPhaseBadge(sb, pixel, small, card.MovementPhase, tx, ty);
        ty += LINE_H;

        // Stats row: Spd  Pwr  Def
        string stats = $"S:{card.BaseSpeed:+#;-#;0} P:{card.BasePower} D:{card.BaseDefense}";
        sb.DrawString(small, stats, new Vector2(tx, ty), new Color(180, 180, 200));
        ty += LINE_H;

        // Movement row
        if (card.MaxMovement > 0)
        {
            string mvType = card.BaseMovementType switch
            {
                MovementType.Approach => ">",
                MovementType.Retreat  => "<",
                MovementType.Free     => "*",
                _                     => "-",
            };
            string mv = card.MinMovement == card.MaxMovement
                ? $"Mv:{mvType}{card.MaxMovement}"
                : $"Mv:{mvType}{card.MinMovement}-{card.MaxMovement}";
            sb.DrawString(small, mv, new Vector2(tx, ty), new Color(160, 200, 160));
            ty += LINE_H;
        }

        // Keywords
        DrawKeywords(sb, small, card.Keywords, tx, ty);
    }

    /// <summary>
    /// Draw a unique card widget.
    /// </summary>
    public static void DrawUnique(
        SpriteBatch sb, Texture2D pixel, SpriteFont font, SpriteFont small,
        UniqueCard card, FighterInstance owner,
        int x, int y,
        bool selected, bool available,
        int cooldown = 0,
        CardPair? pairContext = null)   // if set, uses combined range
    {
        Color artColor  = new Color(60, 40, 80);
        Color border    = selected  ? Color.Yellow :
                          !available ? new Color(60, 60, 60) : new Color(110, 80, 140);
        Color nameTint  = !available ? Color.DimGray : Color.White;

        DrawFrame(sb, pixel, x, y, border);
        DrawArtPlaceholder(sb, pixel, x, y, artColor);

        int tx = x + PADDING;
        int ty = y + ART_H + PADDING;

        string name = AsciiFilter(card.Name);
        if (cooldown > 0) name += $" [{cooldown}]";
        DrawTruncated(sb, small, name, tx, ty, WIDGET_W - PADDING * 2, nameTint);
        ty += LINE_H;

        // Phase badges: attack + post-move
        DrawPhaseBadge(sb, pixel, small, card.AttackPhase, tx, ty);
        int badgeW = BadgeWidth(card.AttackPhase);
        if (card.PostMovementPhase != card.AttackPhase && card.MaxMovement > 0)
            DrawPhaseBadge(sb, pixel, small, card.PostMovementPhase, tx + badgeW + 3, ty);
        ty += LINE_H;

        // Stats: Spd  Pwr  Def  Rng
        int minR = pairContext?.EffectiveMinRange ?? card.MinRange;
        int maxR = pairContext?.EffectiveMaxRange ?? card.MaxRange;
        string rng = minR == maxR ? $"{minR}" : $"{minR}-{maxR}";
        string stats = $"S:{card.BaseSpeed:+#;-#;0} P:{card.BasePower} D:{card.BaseDefense} R:{rng}";
        sb.DrawString(small, stats, new Vector2(tx, ty), new Color(180, 180, 200));
        ty += LINE_H;

        // Target
        string aim = card.PrimaryTarget == card.SecondaryTarget
            ? AsciiFilter($"{card.PrimaryTarget}")
            : AsciiFilter($"{card.PrimaryTarget}/{card.SecondaryTarget}");
        sb.DrawString(small, $"Aim:{aim}", new Vector2(tx, ty), new Color(200, 170, 120));
        ty += LINE_H;

        DrawKeywords(sb, small, card.Keywords, tx, ty);
    }

    /// <summary>
    /// Draw a special card widget.
    /// </summary>
    public static void DrawSpecial(
        SpriteBatch sb, Texture2D pixel, SpriteFont font, SpriteFont small,
        SpecialCard card, FighterInstance owner,
        int x, int y,
        bool selected, bool available,
        int cooldown = 0)
    {
        Color artColor = new Color(70, 30, 30);
        Color border   = selected  ? Color.Yellow :
                         !available ? new Color(60, 60, 60) : new Color(180, 60, 60);
        Color nameTint = !available ? Color.DimGray : Color.White;

        DrawFrame(sb, pixel, x, y, border);
        DrawArtPlaceholder(sb, pixel, x, y, artColor);

        int tx = x + PADDING;
        int ty = y + ART_H + PADDING;

        string name = AsciiFilter(card.Name);
        if (cooldown > 0) name += $" [{cooldown}]";
        DrawTruncated(sb, small, name, tx, ty, WIDGET_W - PADDING * 2, nameTint);
        ty += LINE_H;

        DrawPhaseBadge(sb, pixel, small, card.AttackPhase, tx, ty);
        ty += LINE_H;

        string rng = card.MinRange == card.MaxRange ? $"{card.MinRange}" : $"{card.MinRange}-{card.MaxRange}";
        string stats = $"S:{card.BaseSpeed:+#;-#;0} P:{card.BasePower} D:{card.BaseDefense} R:{rng}";
        sb.DrawString(small, stats, new Vector2(tx, ty), new Color(180, 180, 200));
        ty += LINE_H;

        string aim = card.PrimaryTarget == card.SecondaryTarget
            ? AsciiFilter($"{card.PrimaryTarget}")
            : AsciiFilter($"{card.PrimaryTarget}/{card.SecondaryTarget}");
        sb.DrawString(small, $"Aim:{aim}", new Vector2(tx, ty), new Color(200, 170, 120));
        ty += LINE_H;

        DrawKeywords(sb, small, card.Keywords, tx, ty);
    }

    // ── Compact variants (no art block, COMPACT_H tall) ────────────────────

    public static void DrawGenericCompact(
        SpriteBatch sb, Texture2D pixel, SpriteFont font, SpriteFont small,
        GenericCard card, FighterInstance owner,
        int x, int y,
        bool selected, bool available,
        int cooldown = 0)
    {
        Color border   = selected   ? Color.Yellow
                       : !available ? new Color(60, 60, 60)
                                    : new Color(80, 100, 130);
        Color nameTint = !available ? Color.DimGray : Color.White;
        DrawCompactFrame(sb, pixel, x, y, border);

        int tx = x + PADDING, ty = y + PADDING;
        string name = AsciiFilter(card.Name);
        if (cooldown > 0) name += $" [{cooldown}]";
        DrawTruncated(sb, small, name, tx, ty, WIDGET_W - PADDING * 2, nameTint); ty += LINE_H;

        DrawPhaseBadge(sb, pixel, small, card.MovementPhase, tx, ty); ty += LINE_H;

        string mvType = card.BaseMovementType switch
        {
            MovementType.Approach => ">", MovementType.Retreat => "<",
            MovementType.Free => "*", _ => "-",
        };
        string stats = $"S:{card.BaseSpeed:+#;-#;0} P:{card.BasePower} D:{card.BaseDefense}";
        if (card.MaxMovement > 0) stats += $" {mvType}{card.MaxMovement}";
        sb.DrawString(small, stats, new Vector2(tx, ty), new Color(180, 180, 200)); ty += LINE_H;

        DrawKeywords(sb, small, card.Keywords, tx, ty);
    }

    public static void DrawUniqueCompact(
        SpriteBatch sb, Texture2D pixel, SpriteFont font, SpriteFont small,
        UniqueCard card, FighterInstance owner,
        int x, int y,
        bool selected, bool available,
        int cooldown = 0)
    {
        Color border   = selected   ? Color.Yellow
                       : !available ? new Color(60, 60, 60)
                                    : new Color(110, 80, 140);
        Color nameTint = !available ? Color.DimGray : Color.White;
        DrawCompactFrame(sb, pixel, x, y, border);

        int tx = x + PADDING, ty = y + PADDING;
        string name = AsciiFilter(card.Name);
        if (cooldown > 0) name += $" [{cooldown}]";
        DrawTruncated(sb, small, name, tx, ty, WIDGET_W - PADDING * 2, nameTint); ty += LINE_H;

        DrawPhaseBadge(sb, pixel, small, card.AttackPhase, tx, ty); ty += LINE_H;

        string rng = card.MinRange == card.MaxRange ? $"{card.MinRange}" : $"{card.MinRange}-{card.MaxRange}";
        string stats = $"S:{card.BaseSpeed:+#;-#;0} P:{card.BasePower} D:{card.BaseDefense} R:{rng}";
        sb.DrawString(small, stats, new Vector2(tx, ty), new Color(180, 180, 200)); ty += LINE_H;

        DrawKeywords(sb, small, card.Keywords, tx, ty);
    }

    public static void DrawSpecialCompact(
        SpriteBatch sb, Texture2D pixel, SpriteFont font, SpriteFont small,
        SpecialCard card, FighterInstance owner,
        int x, int y,
        bool selected, bool available,
        int cooldown = 0)
    {
        Color border   = selected   ? Color.Yellow
                       : !available ? new Color(60, 60, 60)
                                    : new Color(180, 60, 60);
        Color nameTint = !available ? Color.DimGray : Color.White;
        DrawCompactFrame(sb, pixel, x, y, border);

        int tx = x + PADDING, ty = y + PADDING;
        string name = AsciiFilter(card.Name);
        if (cooldown > 0) name += $" [{cooldown}]";
        DrawTruncated(sb, small, name, tx, ty, WIDGET_W - PADDING * 2, nameTint); ty += LINE_H;

        DrawPhaseBadge(sb, pixel, small, card.AttackPhase, tx, ty); ty += LINE_H;

        string rng = card.MinRange == card.MaxRange ? $"{card.MinRange}" : $"{card.MinRange}-{card.MaxRange}";
        string stats = $"S:{card.BaseSpeed:+#;-#;0} P:{card.BasePower} D:{card.BaseDefense} R:{rng}";
        sb.DrawString(small, stats, new Vector2(tx, ty), new Color(180, 180, 200)); ty += LINE_H;

        DrawKeywords(sb, small, card.Keywords, tx, ty);
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private static void DrawFrame(SpriteBatch sb, Texture2D pixel, int x, int y, Color border)
    {
        // Background
        sb.Draw(pixel, new Rectangle(x, y, WIDGET_W, WIDGET_H), new Color(20, 20, 30));
        // Border (1px)
        sb.Draw(pixel, new Rectangle(x,              y,              WIDGET_W, 1),         border);
        sb.Draw(pixel, new Rectangle(x,              y + WIDGET_H - 1, WIDGET_W, 1),       border);
        sb.Draw(pixel, new Rectangle(x,              y,              1, WIDGET_H),         border);
        sb.Draw(pixel, new Rectangle(x + WIDGET_W - 1, y,           1, WIDGET_H),         border);
    }

    private static void DrawCompactFrame(SpriteBatch sb, Texture2D pixel, int x, int y, Color border)
    {
        sb.Draw(pixel, new Rectangle(x, y, WIDGET_W, COMPACT_H), new Color(20, 20, 30));
        sb.Draw(pixel, new Rectangle(x,              y,               WIDGET_W, 1),          border);
        sb.Draw(pixel, new Rectangle(x,              y + COMPACT_H - 1, WIDGET_W, 1),        border);
        sb.Draw(pixel, new Rectangle(x,              y,               1, COMPACT_H),         border);
        sb.Draw(pixel, new Rectangle(x + WIDGET_W - 1, y,            1, COMPACT_H),         border);
    }

    private static void DrawArtPlaceholder(SpriteBatch sb, Texture2D pixel, int x, int y, Color fill)
    {
        sb.Draw(pixel, new Rectangle(x + 1, y + 1, WIDGET_W - 2, ART_H - 1), fill);
    }

    private static void DrawPhaseBadge(
        SpriteBatch sb, Texture2D pixel, SpriteFont small,
        TurnPhase phase, int x, int y)
    {
        string label = phase switch
        {
            TurnPhase.Beginning => "BEG",
            TurnPhase.Main      => "MAIN",
            TurnPhase.Finish    => "FINAL",
            TurnPhase.Start     => "START",
            TurnPhase.End       => "END",
            _                   => "?",
        };
        Color bg = phase switch
        {
            TurnPhase.Beginning => ColBeginning,
            TurnPhase.Main      => ColMain,
            TurnPhase.Finish    => ColFinal,
            TurnPhase.Start     => ColStart,
            TurnPhase.End       => ColEnd,
            _                   => Color.Gray,
        };
        Vector2 sz = small.MeasureString(label);
        int pw = (int)sz.X + 6;
        int ph = (int)sz.Y + 2;
        sb.Draw(pixel, new Rectangle(x, y, pw, ph), bg * 0.85f);
        sb.DrawString(small, label, new Vector2(x + 3, y + 1), Color.White);
    }

    private static int BadgeWidth(TurnPhase phase) => phase switch
    {
        TurnPhase.Beginning => 34,
        TurnPhase.Main      => 38,
        TurnPhase.Finish    => 42,
        TurnPhase.Start     => 40,
        TurnPhase.End       => 32,
        _                   => 30,
    };

    private static void DrawKeywords(SpriteBatch sb, SpriteFont small, List<CardKeywordValue> kws, int x, int y)
    {
        if (kws.Count == 0) return;
        string line = string.Join(" ", kws.Select(k => k.Value > 0 ? $"{k.Keyword}({k.Value})" : $"{k.Keyword}"));
        DrawTruncated(sb, small, AsciiFilter(line), x, y, WIDGET_W - PADDING * 2, new Color(190, 150, 255));
    }

    private static void DrawTruncated(SpriteBatch sb, SpriteFont font, string text, int x, int y, int maxW, Color col)
    {
        // Trim string until it fits
        while (text.Length > 1 && font.MeasureString(text).X > maxW)
            text = text[..^1];
        sb.DrawString(font, text, new Vector2(x, y), col);
    }

    private static string AsciiFilter(string s)
    {
        var buf = new System.Text.StringBuilder(s.Length);
        foreach (char c in s)
            if (c >= 32 && c <= 126) buf.Append(c);
        return buf.ToString();
    }
}
