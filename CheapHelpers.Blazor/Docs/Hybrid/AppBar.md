# Blazor App Bar Component

A flexible, customizable app bar component for Blazor Hybrid applications with automatic status bar height detection and programmatic control.

## Overview

The `AppBar` component provides an application-level navigation bar that is **completely separate** from system status bar configuration. It offers:

- **Default implementation** with title and actions for quick setup
- **RenderFragment support** for complete customization
- **Automatic status bar adjustment** for proper spacing
- **Programmatic control** via `IAppBarService`
- **Responsive design** with Material Design principles
- **CSS customization** with built-in themes

## Architecture

```
┌─────────────────────────────────────┐
│     System Status Bar (OS Level)    │  ← AndroidSystemBarsHelper
├─────────────────────────────────────┤
│       App Bar (Application)         │  ← AppBar Component
├─────────────────────────────────────┤
│                                     │
│        Page Content                 │
│                                     │
└─────────────────────────────────────┘
```

**Key Principle:** Status bar and app bar are separate concerns. Status bar is OS-level UI, app bar is application UI.

## Installation

```bash
dotnet add package CheapHelpers.Blazor
```

## Setup

### 1. Register the Service

In your `Program.cs` or `MauiProgram.cs`:

```csharp
using CheapHelpers.Blazor.Hybrid.Extensions;

builder.Services.AddAppBar(options =>
{
    options.DefaultBackgroundColor = "#6200EE";
    options.DefaultTextColor = "#FFFFFF";
    options.DefaultElevation = true;
    options.DefaultHeight = 56;
    options.DefaultAdjustForStatusBar = true;
});
```

### 2. Include the CSS

Add to your `index.html` or `_Layout.cshtml`:

```html
<link href="_content/CheapHelpers.Blazor/css/appbar.css" rel="stylesheet" />
```

### 3. Add Component Imports

In your `_Imports.razor`:

```razor
@using CheapHelpers.Blazor.Hybrid.Components
@using CheapHelpers.Blazor.Hybrid.Services
```

## Usage Levels

### Level 1: Default App Bar (Simplest)

Just provide a title for a clean, simple app bar:

```razor
<AppBar Title="My Application" />
```

**Result:** A Material Design app bar with your title and automatic status bar spacing.

### Level 2: App Bar with Actions

Add action buttons or controls on the right:

```razor
<AppBar Title="My Application">
    <Actions>
        <button @onclick="OnSearch">
            <i class="fas fa-search"></i>
        </button>
        <button @onclick="OnMenu">
            <i class="fas fa-bars"></i>
        </button>
    </Actions>
</AppBar>
```

**Result:** App bar with title on the left, your custom actions on the right.

### Level 3: Fully Custom Content

Complete control over the entire app bar:

```razor
<AppBar>
    <Content>
        <div style="display: flex; align-items: center; width: 100%; padding: 8px 16px;">
            <img src="logo.png" style="height: 40px;" />
            <span style="margin-left: 16px; font-size: 20px;">My Brand</span>
            <div style="margin-left: auto; display: flex; gap: 12px;">
                <button class="custom-btn">Login</button>
                <button class="custom-btn">Sign Up</button>
            </div>
        </div>
    </Content>
</AppBar>
```

**Result:** Your completely custom app bar layout with full design control.

### Level 4: Programmatic Control

Control the app bar from code-behind or other components:

```razor
@inject IAppBarService AppBarService

@code {
    protected override void OnInitialized()
    {
        // Set title dynamically
        AppBarService.SetTitle("Dynamic Title");

        // Show/hide app bar
        AppBarService.SetVisible(true);

        // Change colors
        AppBarService.SetBackgroundColor("#03DAC6");
        AppBarService.SetTextColor("#000000");

        // Listen for changes
        AppBarService.OnAppBarChanged += HandleAppBarChange;
    }

    private void HandleAppBarChange()
    {
        Console.WriteLine("App bar state changed");
        StateHasChanged();
    }

    public void Dispose()
    {
        AppBarService.OnAppBarChanged -= HandleAppBarChange;
    }
}
```

## Component Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string?` | `null` | Title text to display (ignored if Content is provided) |
| `Content` | `RenderFragment?` | `null` | Custom content that overrides default layout |
| `Actions` | `RenderFragment?` | `null` | Action buttons to display on the right |
| `Height` | `double?` | `56` | Custom height in pixels (excluding status bar) |
| `CssClass` | `string?` | `null` | Additional CSS classes |
| `AdjustForStatusBar` | `bool` | `true` | Auto-adjust for status bar height |
| `BackgroundColor` | `string` | `"#6200EE"` | Background color (CSS value) |
| `TextColor` | `string` | `"#FFFFFF"` | Text color (CSS value) |
| `Elevated` | `bool` | `true` | Apply shadow/elevation |

## IAppBarService API

### Methods

```csharp
// Content management
void SetContent(RenderFragment content);
void SetTitle(string title);
void SetActions(RenderFragment? actions);
void Clear();

// Visibility
void SetVisible(bool visible);
bool IsVisible { get; }

// Styling
void SetBackgroundColor(string color);
void SetTextColor(string color);
string BackgroundColor { get; }
string TextColor { get; }

// Layout
double GetStatusBarHeight();

// State
string? CurrentTitle { get; }
RenderFragment? CurrentContent { get; }
RenderFragment? CurrentActions { get; }

// Events
event Action? OnAppBarChanged;
```

## Styling and Customization

### Built-in CSS Classes

```css
.cheap-appbar              /* Base app bar container */
.cheap-appbar-content      /* Content wrapper */
.cheap-appbar-title        /* Title text */
.cheap-appbar-actions      /* Actions container */
.cheap-appbar-spacer       /* Content spacer below app bar */
.cheap-appbar-offset       /* Utility class for page padding */
```

### Theme Variants

```razor
<!-- Primary theme (default) -->
<AppBar Title="Primary" CssClass="primary" />

<!-- Secondary theme -->
<AppBar Title="Secondary" CssClass="secondary" />

<!-- Dark theme -->
<AppBar Title="Dark Mode" CssClass="dark" />

<!-- Light theme -->
<AppBar Title="Light Mode" CssClass="light" />
```

### Elevation Levels

```razor
<!-- Material Design elevation levels -->
<AppBar Title="Level 1" CssClass="elevation-1" />
<AppBar Title="Level 2" CssClass="elevation-2" />
<AppBar Title="Level 3" CssClass="elevation-3" />
<AppBar Title="Level 4" CssClass="elevation-4" />
```

### Custom Styling

Override CSS variables in your own stylesheet:

```css
.my-custom-appbar {
    --appbar-height: 64px;
    --appbar-background: linear-gradient(90deg, #667eea 0%, #764ba2 100%);
    --appbar-shadow: 0 4px 6px rgba(0,0,0,0.2);
}
```

```razor
<AppBar Title="Custom" CssClass="my-custom-appbar" />
```

## Complete Examples

### Example 1: E-commerce App

```razor
<AppBar BackgroundColor="#FF6B6B" TextColor="#FFFFFF">
    <Content>
        <div class="ecommerce-appbar">
            <button @onclick="ToggleMenu" class="menu-btn">
                <i class="fas fa-bars"></i>
            </button>
            <div class="logo">ShopApp</div>
            <div class="search-box">
                <input type="search" placeholder="Search products..." />
            </div>
            <div class="actions">
                <button @onclick="GoToCart">
                    <i class="fas fa-shopping-cart"></i>
                    @if (CartCount > 0)
                    {
                        <span class="badge">@CartCount</span>
                    }
                </button>
            </div>
        </div>
    </Content>
</AppBar>

@code {
    private int CartCount = 3;

    private void ToggleMenu() { /* ... */ }
    private void GoToCart() { /* ... */ }
}
```

### Example 2: Settings Page with Dynamic Title

```razor
@page "/settings"
@inject IAppBarService AppBarService

<AppBar />

<div class="cheap-appbar-offset">
    <h2>Settings Content</h2>
    <!-- Your settings content -->
</div>

@code {
    protected override void OnInitialized()
    {
        AppBarService.SetTitle("Settings");
        AppBarService.SetActions(builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, SaveSettings));
            builder.AddContent(2, "Save");
            builder.CloseElement();
        });
    }

    private void SaveSettings()
    {
        // Save logic
    }
}
```

### Example 3: Conditional App Bar

```razor
@inject IAppBarService AppBarService
@inject NavigationManager Navigation

<AppBar />

@code {
    protected override void OnInitialized()
    {
        Navigation.LocationChanged += OnLocationChanged;
        UpdateAppBarForCurrentPage();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        UpdateAppBarForCurrentPage();
    }

    private void UpdateAppBarForCurrentPage()
    {
        var path = Navigation.ToBaseRelativePath(Navigation.Uri);

        if (path.StartsWith("login") || path.StartsWith("register"))
        {
            // Hide app bar on auth pages
            AppBarService.SetVisible(false);
        }
        else if (path.StartsWith("profile"))
        {
            AppBarService.SetVisible(true);
            AppBarService.SetTitle("Profile");
            AppBarService.SetBackgroundColor("#6200EE");
        }
        else
        {
            AppBarService.SetVisible(true);
            AppBarService.SetTitle("Home");
            AppBarService.SetBackgroundColor("#03DAC6");
        }
    }

    public void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
    }
}
```

### Example 4: Search-enabled App Bar

```razor
@inject IAppBarService AppBarService

<AppBar>
    <Content>
        <div class="search-appbar">
            @if (IsSearching)
            {
                <button @onclick="CancelSearch" class="back-btn">
                    <i class="fas fa-arrow-left"></i>
                </button>
                <input type="search"
                       @bind="SearchQuery"
                       @bind:event="oninput"
                       placeholder="Search..."
                       class="search-input" />
                <button @onclick="ClearSearch" class="clear-btn">
                    <i class="fas fa-times"></i>
                </button>
            }
            else
            {
                <div class="title">@Title</div>
                <button @onclick="StartSearch" class="search-btn">
                    <i class="fas fa-search"></i>
                </button>
            }
        </div>
    </Content>
</AppBar>

@code {
    private bool IsSearching = false;
    private string SearchQuery = "";
    private string Title = "My App";

    private void StartSearch()
    {
        IsSearching = true;
        StateHasChanged();
    }

    private void CancelSearch()
    {
        IsSearching = false;
        SearchQuery = "";
        StateHasChanged();
    }

    private void ClearSearch()
    {
        SearchQuery = "";
    }
}
```

## Integration with System Status Bar

### Android

Configure system status bar separately using `AndroidSystemBarsHelper`:

```csharp
// In MainActivity.cs
using CheapHelpers.MAUI.Helpers;

protected override void OnCreate(Bundle? savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    // Configure system status bar (OS level)
    AndroidSystemBarsHelper.ConfigureTransparentStatusBar(this, lightStatusBar: true);
}
```

Then use the app bar component in your Blazor UI:

```razor
<!-- In your Blazor layout (Application level) -->
<AppBar Title="My App" BackgroundColor="#6200EE" />
```

### iOS

Configure status bar in `Info.plist`:

```xml
<key>UIViewControllerBasedStatusBarAppearance</key>
<false/>
<key>UIStatusBarStyle</key>
<string>UIStatusBarStyleLightContent</string>
```

App bar will automatically detect and adjust for the iOS status bar height.

## Best Practices

1. **Separation of Concerns**
   - Use `AndroidSystemBarsHelper` / iOS APIs for **system** status bar
   - Use `AppBar` component for **application** navigation bar
   - Never mix the two concerns

2. **State Management**
   - Use `IAppBarService` for centralized control
   - Subscribe to `OnAppBarChanged` for reactive updates
   - Always unsubscribe in `Dispose()`

3. **Performance**
   - Use `@key` directive when dynamically changing app bar content
   - Avoid heavy computations in `Content` RenderFragment
   - Cache action buttons if they don't change frequently

4. **Responsive Design**
   - Test on different screen sizes
   - Use CSS media queries for mobile-specific styles
   - Consider collapsing actions into a menu on small screens

5. **Accessibility**
   - Provide `aria-label` for icon buttons
   - Ensure sufficient color contrast (WCAG AA: 4.5:1)
   - Support keyboard navigation

## Troubleshooting

### App Bar Hidden Behind Status Bar

**Problem:** App bar is partially hidden by the system status bar.

**Solution:** Ensure `AdjustForStatusBar` is `true` (default):

```razor
<AppBar Title="My App" AdjustForStatusBar="true" />
```

### Content Overlapping App Bar

**Problem:** Page content is rendered behind the app bar.

**Solution:** Use the offset utility class:

```razor
<AppBar Title="My App" />

<div class="cheap-appbar-offset">
    <!-- Your page content -->
</div>
```

Or manually add padding:

```css
.page-content {
    padding-top: 80px; /* Adjust based on your app bar height */
}
```

### App Bar Not Updating

**Problem:** Changes to `IAppBarService` don't reflect in UI.

**Solution:** Ensure you're calling `StateHasChanged()` after subscribing to events:

```csharp
protected override void OnInitialized()
{
    AppBarService.OnAppBarChanged += () =>
    {
        InvokeAsync(StateHasChanged); // Important!
    };
}
```

### Custom Styling Not Applied

**Problem:** Custom CSS classes aren't affecting the app bar.

**Solution:** Ensure CSS specificity is high enough:

```css
/* Not specific enough */
.my-appbar { background: red; }

/* Better */
.cheap-appbar.my-appbar { background: red; }

/* Most specific */
div.cheap-appbar.my-appbar { background: red !important; }
```

## See Also

- [Android System Bars](../../../CheapHelpers.MAUI/Docs/AndroidSystemBars.md) - System-level status bar configuration
- [Status Bar Helper](../../../CheapHelpers.MAUI/Docs/StatusBar.md) - Cross-platform status bar utilities
- [Blazor Hybrid](Hybrid.md) - Other Blazor Hybrid features
- [Components](../Components.md) - Other CheapHelpers Blazor components

## Source Location

- Component: `CheapHelpers.Blazor/Hybrid/Components/AppBar.razor`
- Service: `CheapHelpers.Blazor/Hybrid/Services/AppBarService.cs`
- Styles: `CheapHelpers.Blazor/wwwroot/css/appbar.css`
