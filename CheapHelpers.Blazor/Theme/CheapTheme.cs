using MudBlazor;

namespace CheapHelpers.Blazor.Theme;

/// <summary>
/// Builds the house MudBlazor theme: flat surfaces split by hairlines, cards that lift off the field
/// in both modes, a drawer that stays dark in both modes and dark-mode text as an opacity ladder.
/// </summary>
public static class CheapTheme
{
    private const string DefaultBorderRadius = "12px";
    private const string DrawerWidthLeft = "260px";

    private const string LightAppbarText = "#1a1a1a";
    private const string LightLines = "#E3E3E7";
    private const string LightTableLines = "#ECECEF";

    private const string DrawerText = "rgba(255,255,255,0.70)";
    private const string DarkTextPrimary = "rgba(255,255,255,0.92)";
    private const string DarkTextSecondary = "rgba(255,255,255,0.62)";
    private const string DarkLines = "rgba(255,255,255,0.12)";
    private const string DarkTableLines = "rgba(255,255,255,0.10)";

    private const int SnackbarVisibleMilliseconds = 2000;
    private const int SnackbarTransitionMilliseconds = 200;

    /// <summary>
    /// Creates the house theme from the app's colours and its two font stacks.
    /// </summary>
    /// <param name="colors">The app-specific colours.</param>
    /// <param name="displayFonts">Font stack for H4, H5 and H6.</param>
    /// <param name="bodyFonts">Font stack for everything else.</param>
    public static MudTheme Create(CheapThemeColors colors, string[] displayFonts, string[] bodyFonts) => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = colors.Primary,
            Secondary = colors.Secondary,
            Background = colors.LightField,
            Surface = colors.LightCard,
            AppbarBackground = colors.LightCard,
            AppbarText = LightAppbarText,
            DrawerBackground = colors.Drawer,
            DrawerText = DrawerText,
            DrawerIcon = DrawerText,
            // MudBlazor's default lines are too heavy for the flat look
            LinesDefault = LightLines,
            TableLines = LightTableLines,
            Divider = LightLines
        },
        PaletteDark = new PaletteDark
        {
            Primary = colors.Primary,
            Secondary = colors.Secondary,
            Background = colors.DarkField,
            Surface = colors.DarkCard,
            AppbarBackground = colors.DarkField,
            DrawerBackground = colors.Drawer,
            DrawerText = DrawerText,
            DrawerIcon = DrawerText,
            TextPrimary = DarkTextPrimary,
            TextSecondary = DarkTextSecondary,
            ActionDefault = DarkTextSecondary,
            LinesDefault = DarkLines,
            TableLines = DarkTableLines,
            Divider = DarkLines
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = DefaultBorderRadius,
            DrawerWidthLeft = DrawerWidthLeft
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = bodyFonts },
            H4 = new H4Typography { FontFamily = displayFonts },
            H5 = new H5Typography { FontFamily = displayFonts },
            H6 = new H6Typography { FontFamily = displayFonts }
        }
    };

    /// <summary>
    /// Applies the house snackbar defaults (bottom right, 2 s visible, 200 ms transitions) and returns the same instance.
    /// </summary>
    /// <param name="config">The configuration from <c>AddMudServices</c>.</param>
    public static SnackbarConfiguration ConfigureSnackbar(SnackbarConfiguration config)
    {
        config.PositionClass = Defaults.Classes.Position.BottomRight;
        config.VisibleStateDuration = SnackbarVisibleMilliseconds;
        config.ShowTransitionDuration = SnackbarTransitionMilliseconds;
        config.HideTransitionDuration = SnackbarTransitionMilliseconds;
        return config;
    }
}
