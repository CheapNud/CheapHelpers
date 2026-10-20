namespace CheapHelpers.Blazor.Theme;

/// <summary>
/// The app-specific colours <see cref="CheapTheme.Create"/> needs, each as a hex string.
/// </summary>
/// <param name="Primary">Primary brand colour, used in both modes.</param>
/// <param name="Secondary">Secondary brand colour, used in both modes.</param>
/// <param name="LightField">Light-mode page background (off-white, darker than <paramref name="LightCard"/>).</param>
/// <param name="LightCard">Light-mode card surface (usually white).</param>
/// <param name="DarkField">Dark-mode page background.</param>
/// <param name="DarkCard">Dark-mode card surface, lighter than <paramref name="DarkField"/>.</param>
/// <param name="Drawer">Drawer background, used in both modes; keep it dark.</param>
public sealed record CheapThemeColors(
    string Primary,
    string Secondary,
    string LightField,
    string LightCard,
    string DarkField,
    string DarkCard,
    string Drawer);
