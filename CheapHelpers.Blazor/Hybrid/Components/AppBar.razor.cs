using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CheapHelpers.Blazor.Hybrid.Components;

/// <summary>
/// App bar component for Blazor Hybrid applications with automatic status bar height detection.
/// Provides default implementation with title and actions, plus full customization via RenderFragment.
/// </summary>
/// <remarks>
/// <para>
/// This component is completely separate from system status bar configuration.
/// It provides an application-level navigation bar that automatically adjusts for the status bar.
/// </para>
/// <para>
/// <b>Usage Levels:</b>
/// <list type="number">
/// <item>Default: Just provide a Title</item>
/// <item>With Actions: Provide Title and Actions RenderFragment</item>
/// <item>Fully Custom: Provide Content RenderFragment for complete control</item>
/// </list>
/// </para>
/// </remarks>
public partial class AppBar : ComponentBase, IDisposable
{
    /// <summary>
    /// Complete custom content for the app bar. When provided, overrides Title and Actions.
    /// </summary>
    [Parameter]
    public RenderFragment? Content { get; set; }

    /// <summary>
    /// Action buttons or controls to display on the right side of the app bar.
    /// Only used when Content is null.
    /// </summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>
    /// Title text to display in the app bar.
    /// Only used when Content is null.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// Custom height for the app bar in pixels.
    /// If null, uses default height (56px) plus status bar height when AdjustForStatusBar is true.
    /// </summary>
    [Parameter]
    public double? Height { get; set; }

    /// <summary>
    /// Additional CSS classes to apply to the app bar.
    /// </summary>
    [Parameter]
    public string? CssClass { get; set; }

    /// <summary>
    /// Whether to automatically adjust height and padding for the status bar.
    /// Default is true.
    /// </summary>
    [Parameter]
    public bool AdjustForStatusBar { get; set; } = true;

    /// <summary>
    /// Background color for the app bar. Supports any valid CSS color value.
    /// </summary>
    [Parameter]
    public string BackgroundColor { get; set; } = "#6200EE";

    /// <summary>
    /// Text color for the app bar. Supports any valid CSS color value.
    /// </summary>
    [Parameter]
    public string TextColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Whether to apply a shadow/elevation to the app bar.
    /// Default is true.
    /// </summary>
    [Parameter]
    public bool Elevated { get; set; } = true;

    [Inject]
    private IJSRuntime? JSRuntime { get; set; }

    private double _statusBarHeight = 0;
    private const double DefaultAppBarHeight = 56;

    protected override async Task OnInitializedAsync()
    {
        if (AdjustForStatusBar && JSRuntime != null)
        {
            try
            {
                // Try to get status bar height from JS interop
                _statusBarHeight = await JSRuntime.InvokeAsync<double>("eval",
                    "window.statusBarHeight || 0");
            }
            catch
            {
                // Fallback: use common status bar height
                _statusBarHeight = 24;
            }
        }

        await base.OnInitializedAsync();
    }

    /// <summary>
    /// Generates the inline style string for the app bar.
    /// </summary>
    private string GetStyle()
    {
        var styles = new List<string>();

        // Calculate total height
        var totalHeight = Height ?? DefaultAppBarHeight;
        if (AdjustForStatusBar)
        {
            totalHeight += _statusBarHeight;
            styles.Add($"padding-top: {_statusBarHeight}px");
        }

        styles.Add($"height: {totalHeight}px");
        styles.Add($"background-color: {BackgroundColor}");
        styles.Add($"color: {TextColor}");

        if (Elevated)
        {
            styles.Add("box-shadow: 0 2px 4px rgba(0,0,0,0.1)");
        }

        return string.Join("; ", styles);
    }

    /// <summary>
    /// Gets the current status bar height in pixels.
    /// </summary>
    public double GetStatusBarHeight() => _statusBarHeight;

    /// <summary>
    /// Gets the total app bar height including status bar adjustment.
    /// </summary>
    public double GetTotalHeight()
    {
        var totalHeight = Height ?? DefaultAppBarHeight;
        if (AdjustForStatusBar)
        {
            totalHeight += _statusBarHeight;
        }
        return totalHeight;
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
