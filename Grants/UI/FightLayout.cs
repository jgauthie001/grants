namespace Grants.UI;

/// <summary>
/// Derives proportional panel positions from the current viewport dimensions.
/// Allows FightScreen draw methods to adapt to any resolution without hardcoded
/// pixel coordinates. Construct once per frame from the current viewport size.
/// </summary>
public readonly struct FightLayout
{
    /// <summary>Horizontal scale relative to the 1280-wide design baseline.</summary>
    public float Sx { get; }

    /// <summary>Vertical scale relative to the 720-high design baseline.</summary>
    public float Sy { get; }

    /// <summary>Left margin X for player-side panels (fixed 20px margin).</summary>
    public int LeftX { get; }

    /// <summary>X coordinate where the right opponent panel begins.</summary>
    public int RightX { get; }

    /// <summary>Pixel X coordinate of the hex board center.</summary>
    public float BoardCenterX { get; }

    /// <summary>Pixel Y coordinate of the hex board center.</summary>
    public float BoardCenterY { get; }

    /// <summary>Hex cell size in pixels, scaled from the 36px baseline.</summary>
    public float HexSize { get; }

    /// <summary>Y coordinate where the round log block begins (~70% down the screen).</summary>
    public int LogY { get; }

    public FightLayout(int viewportW, int viewportH)
    {
        Sx           = viewportW / 1280f;
        Sy           = viewportH / 720f;
        LeftX        = 20;
        RightX       = (int)(viewportW * 0.75f) + 10;
        BoardCenterX = viewportW / 2f;
        BoardCenterY = viewportH / 2f;
        HexSize      = 36f * Sy;
        LogY         = (int)(viewportH * 0.70f);
    }
}
