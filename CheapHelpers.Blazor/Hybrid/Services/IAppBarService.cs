using Microsoft.AspNetCore.Components;

namespace CheapHelpers.Blazor.Hybrid.Services;

/// <summary>
/// Service for managing app bar state and content programmatically.
/// Allows dynamic control of the app bar from anywhere in the application.
/// </summary>
/// <remarks>
/// <para>
/// This service provides a centralized way to control app bar behavior without
/// tight coupling between components. It's completely separate from system status bar configuration.
/// </para>
/// </remarks>
public interface IAppBarService
{
    /// <summary>
    /// Sets custom content for the app bar.
    /// </summary>
    /// <param name="content">RenderFragment to display in the app bar</param>
    void SetContent(RenderFragment content);

    /// <summary>
    /// Sets the title of the app bar.
    /// Clears any custom content.
    /// </summary>
    /// <param name="title">Title text to display</param>
    void SetTitle(string title);

    /// <summary>
    /// Sets action buttons/controls for the app bar.
    /// </summary>
    /// <param name="actions">RenderFragment containing action controls</param>
    void SetActions(RenderFragment? actions);

    /// <summary>
    /// Shows or hides the app bar.
    /// </summary>
    /// <param name="visible">True to show, false to hide</param>
    void SetVisible(bool visible);

    /// <summary>
    /// Gets the current status bar height for layout calculations.
    /// </summary>
    /// <returns>Status bar height in pixels</returns>
    double GetStatusBarHeight();

    /// <summary>
    /// Gets whether the app bar is currently visible.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Gets the current title text.
    /// </summary>
    string? CurrentTitle { get; }

    /// <summary>
    /// Gets the current custom content.
    /// </summary>
    RenderFragment? CurrentContent { get; }

    /// <summary>
    /// Gets the current actions.
    /// </summary>
    RenderFragment? CurrentActions { get; }

    /// <summary>
    /// Event triggered when app bar state changes.
    /// Subscribe to this to react to programmatic app bar changes.
    /// </summary>
    event Action? OnAppBarChanged;

    /// <summary>
    /// Clears all app bar content and actions.
    /// </summary>
    void Clear();

    /// <summary>
    /// Sets the background color of the app bar.
    /// </summary>
    /// <param name="color">CSS color value</param>
    void SetBackgroundColor(string color);

    /// <summary>
    /// Sets the text color of the app bar.
    /// </summary>
    /// <param name="color">CSS color value</param>
    void SetTextColor(string color);

    /// <summary>
    /// Gets the current background color.
    /// </summary>
    string BackgroundColor { get; }

    /// <summary>
    /// Gets the current text color.
    /// </summary>
    string TextColor { get; }
}
