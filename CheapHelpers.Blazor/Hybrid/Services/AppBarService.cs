using Microsoft.AspNetCore.Components;

namespace CheapHelpers.Blazor.Hybrid.Services;

/// <summary>
/// Default implementation of IAppBarService for managing app bar state.
/// </summary>
public class AppBarService : IAppBarService
{
    private bool _isVisible = true;
    private string? _currentTitle;
    private RenderFragment? _currentContent;
    private RenderFragment? _currentActions;
    private double _statusBarHeight;
    private string _backgroundColor = "#6200EE";
    private string _textColor = "#FFFFFF";

    /// <inheritdoc/>
    public event Action? OnAppBarChanged;

    /// <inheritdoc/>
    public bool IsVisible => _isVisible;

    /// <inheritdoc/>
    public string? CurrentTitle => _currentTitle;

    /// <inheritdoc/>
    public RenderFragment? CurrentContent => _currentContent;

    /// <inheritdoc/>
    public RenderFragment? CurrentActions => _currentActions;

    /// <inheritdoc/>
    public string BackgroundColor => _backgroundColor;

    /// <inheritdoc/>
    public string TextColor => _textColor;

    /// <inheritdoc/>
    public void SetContent(RenderFragment content)
    {
        _currentContent = content;
        _currentTitle = null; // Clear title when custom content is set
        NotifyStateChanged();
    }

    /// <inheritdoc/>
    public void SetTitle(string title)
    {
        _currentTitle = title;
        _currentContent = null; // Clear custom content when title is set
        NotifyStateChanged();
    }

    /// <inheritdoc/>
    public void SetActions(RenderFragment? actions)
    {
        _currentActions = actions;
        NotifyStateChanged();
    }

    /// <inheritdoc/>
    public void SetVisible(bool visible)
    {
        if (_isVisible != visible)
        {
            _isVisible = visible;
            NotifyStateChanged();
        }
    }

    /// <inheritdoc/>
    public double GetStatusBarHeight()
    {
        return _statusBarHeight;
    }

    /// <summary>
    /// Internal method to set status bar height from the AppBar component.
    /// </summary>
    internal void SetStatusBarHeight(double height)
    {
        _statusBarHeight = height;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _currentTitle = null;
        _currentContent = null;
        _currentActions = null;
        NotifyStateChanged();
    }

    /// <inheritdoc/>
    public void SetBackgroundColor(string color)
    {
        if (_backgroundColor != color)
        {
            _backgroundColor = color;
            NotifyStateChanged();
        }
    }

    /// <inheritdoc/>
    public void SetTextColor(string color)
    {
        if (_textColor != color)
        {
            _textColor = color;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged()
    {
        OnAppBarChanged?.Invoke();
    }
}
