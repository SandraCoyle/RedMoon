namespace RedMoon.App.Theme;

/// <summary>
/// Appens farver – ÉT sted, så designet kan ændres centralt.
/// Hver farve findes i en lys og en mørk udgave (dark mode).
/// Tekstfarver er valgt med mindst 4.5:1 kontrast mod baggrunden (WCAG 2.1 AA).
/// Stil: natlig himmel, lilla/lyserød frem for knaldrød (jf. brugerresearch).
/// </summary>
public static class Palette
{
    // --- Lys tilstand ---
    public static readonly Color BackgroundLight = Color.FromArgb("#FFF6FA");
    public static readonly Color SurfaceLight = Color.FromArgb("#FFFFFF");
    public static readonly Color SurfaceAltLight = Color.FromArgb("#F5E8F6");
    public static readonly Color BorderLight = Color.FromArgb("#E6D3EA");
    public static readonly Color TextPrimaryLight = Color.FromArgb("#2A1638");
    public static readonly Color TextSecondaryLight = Color.FromArgb("#5F4A6C");
    public static readonly Color AccentLight = Color.FromArgb("#B0175A");
    public static readonly Color OnAccentLight = Color.FromArgb("#FFFFFF");
    public static readonly Color AccentSoftLight = Color.FromArgb("#F9D7E6");
    public static readonly Color PeriodLight = Color.FromArgb("#C2185B");
    public static readonly Color OnPeriodLight = Color.FromArgb("#FFFFFF");
    public static readonly Color PredictedLight = Color.FromArgb("#F6B3CD");
    public static readonly Color MarginLight = Color.FromArgb("#FBE0EB");
    public static readonly Color RingDotLight = Color.FromArgb("#C9B2DD");
    public static readonly Color MoonLitLight = Color.FromArgb("#F4A38C");
    public static readonly Color MoonShadowLight = Color.FromArgb("#3B2350");
    public static readonly Color CraterLight = Color.FromArgb("#E07F66");
    public static readonly Color ErrorLight = Color.FromArgb("#A1001F");

    // --- Mørk tilstand ---
    public static readonly Color BackgroundDark = Color.FromArgb("#140B22");
    public static readonly Color SurfaceDark = Color.FromArgb("#21132F");
    public static readonly Color SurfaceAltDark = Color.FromArgb("#2E1B43");
    public static readonly Color BorderDark = Color.FromArgb("#433059");
    public static readonly Color TextPrimaryDark = Color.FromArgb("#F7EEFF");
    public static readonly Color TextSecondaryDark = Color.FromArgb("#C9B6DC");
    public static readonly Color AccentDark = Color.FromArgb("#FF6FA0");
    public static readonly Color OnAccentDark = Color.FromArgb("#1A0B23");
    public static readonly Color AccentSoftDark = Color.FromArgb("#4A1F3D");
    public static readonly Color PeriodDark = Color.FromArgb("#FF4D86");
    public static readonly Color OnPeriodDark = Color.FromArgb("#1A0B23");
    public static readonly Color PredictedDark = Color.FromArgb("#6B2852");
    public static readonly Color MarginDark = Color.FromArgb("#3D1A38");
    public static readonly Color RingDotDark = Color.FromArgb("#5E4880");
    public static readonly Color MoonLitDark = Color.FromArgb("#F6A58E");
    public static readonly Color MoonShadowDark = Color.FromArgb("#0A0512");
    public static readonly Color CraterDark = Color.FromArgb("#DD7C62");
    public static readonly Color ErrorDark = Color.FromArgb("#FF8A9A");

    /// <summary>Sand hvis appen aktuelt vises i mørk tilstand.</summary>
    public static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;

    /// <summary>Vælger lys eller mørk farve ud fra det aktuelle tema (bruges i tegnekode).</summary>
    public static Color Pick(Color light, Color dark) => IsDark ? dark : light;
}
