# CheapHelpers.Blazor - ClipboardService

Comprehensive guide to async clipboard operations in Blazor Server applications.

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Basic Usage](#basic-usage)
- [API Reference](#api-reference)
- [Examples](#examples)
- [Security Considerations](#security-considerations)
- [Browser Compatibility](#browser-compatibility)
- [Troubleshooting](#troubleshooting)

---

## Overview

`ClipboardService` provides a simple, async wrapper around the browser's Clipboard API for reading and writing text to the user's clipboard. It uses JavaScript interop to call the native `navigator.clipboard` API.

**Key Features:**
- Async read/write operations
- Type-safe API
- Minimal overhead
- Browser-native clipboard access
- Supports all modern browsers

**Limitations:**
- Text only (no images, HTML, or custom formats)
- Requires HTTPS in production
- Requires user permission in most browsers

---

## Installation

`ClipboardService` is included in `CheapHelpers.Blazor`. No additional packages required.

---

## Basic Usage

### Register Service

```csharp
// Program.cs
builder.Services.AddScoped<ClipboardService>();

// Or use CheapHelpers complete setup (includes ClipboardService)
builder.Services.AddCheapHelpersBlazor<ApplicationUser>();
```

### Inject and Use

```razor
@inject ClipboardService Clipboard

<MudTextField @bind-Value="_text" Label="Text to Copy" />
<MudButton OnClick="CopyToClipboard">Copy</MudButton>

<MudButton OnClick="PasteFromClipboard">Paste</MudButton>
<MudText>Pasted: @_pastedText</MudText>

@code {
    private string _text = "Hello, World!";
    private string _pastedText = "";

    private async Task CopyToClipboard()
    {
        await Clipboard.WriteTextAsync(_text);
    }

    private async Task PasteFromClipboard()
    {
        _pastedText = await Clipboard.ReadTextAsync();
    }
}
```

---

## API Reference

### WriteTextAsync

Write text to the clipboard.

```csharp
ValueTask WriteTextAsync(string text)
```

**Parameters:**
- `text` - Text to write to clipboard

**Returns:** `ValueTask`

**Throws:**
- `JSException` - If clipboard access is denied or not supported
- `ArgumentNullException` - If text is null

**Example:**

```csharp
await Clipboard.WriteTextAsync("Copy this text");
```

---

### ReadTextAsync

Read text from the clipboard.

```csharp
ValueTask<string> ReadTextAsync()
```

**Parameters:** None

**Returns:** `ValueTask<string>` - Text from clipboard

**Throws:**
- `JSException` - If clipboard access is denied or not supported

**Example:**

```csharp
var clipboardText = await Clipboard.ReadTextAsync();
```

---

## Examples

### Copy Button with Feedback

```razor
@inject ClipboardService Clipboard
@inject ISnackbar Snackbar

<MudTextField @bind-Value="_apiKey"
              Label="API Key"
              ReadOnly="true"
              Adornment="Adornment.End"
              AdornmentIcon="@Icons.Material.Filled.ContentCopy"
              OnAdornmentClick="CopyApiKey" />

@code {
    private string _apiKey = "sk_live_abc123xyz789";

    private async Task CopyApiKey()
    {
        try
        {
            await Clipboard.WriteTextAsync(_apiKey);
            Snackbar.Add("API key copied to clipboard", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to copy: {ex.Message}", Severity.Error);
        }
    }
}
```

---

### Copy Code Block

```razor
@inject ClipboardService Clipboard

<MudPaper Class="pa-4 position-relative">
    <MudIconButton Icon="@Icons.Material.Filled.ContentCopy"
                   Color="Color.Primary"
                   Size="Size.Small"
                   Class="position-absolute"
                   Style="top: 8px; right: 8px;"
                   OnClick="CopyCode" />

    <pre><code>@_codeBlock</code></pre>
</MudPaper>

@code {
    private string _codeBlock = @"
public async Task<User> GetUserAsync(int id)
{
    return await _context.Users.FindAsync(id);
}";

    private async Task CopyCode()
    {
        await Clipboard.WriteTextAsync(_codeBlock);
    }
}
```

---

### Copy Table Data

```razor
@inject ClipboardService Clipboard
@inject ISnackbar Snackbar

<MudTable Items="_users" Hover="true">
    <ToolBarContent>
        <MudButton StartIcon="@Icons.Material.Filled.ContentCopy"
                   OnClick="CopyTableData"
                   Color="Color.Primary">
            Copy All
        </MudButton>
    </ToolBarContent>
    <HeaderContent>
        <MudTh>Name</MudTh>
        <MudTh>Email</MudTh>
        <MudTh>Role</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>@context.Name</MudTd>
        <MudTd>@context.Email</MudTd>
        <MudTd>@context.Role</MudTd>
    </RowTemplate>
</MudTable>

@code {
    private List<User> _users = new();

    private async Task CopyTableData()
    {
        // Convert table to TSV (Tab-Separated Values)
        var tsv = string.Join("\n", _users.Select(u =>
            $"{u.Name}\t{u.Email}\t{u.Role}"));

        await Clipboard.WriteTextAsync(tsv);
        Snackbar.Add($"Copied {_users.Count} rows", Severity.Success);
    }
}
```

---

### Paste and Validate

```razor
@inject ClipboardService Clipboard

<MudButton OnClick="PasteUrl">Paste URL</MudButton>
<MudTextField @bind-Value="_url" Label="URL" ReadOnly="true" />

@if (!string.IsNullOrEmpty(_errorMessage))
{
    <MudAlert Severity="Severity.Error">@_errorMessage</MudAlert>
}

@code {
    private string _url = "";
    private string _errorMessage = "";

    private async Task PasteUrl()
    {
        _errorMessage = "";

        try
        {
            var text = await Clipboard.ReadTextAsync();

            // Validate URL
            if (Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                _url = text;
            }
            else
            {
                _errorMessage = "Clipboard does not contain a valid URL";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to paste: {ex.Message}";
        }
    }
}
```

---

### Copy Share Link

```razor
@inject ClipboardService Clipboard
@inject NavigationManager Navigation
@inject ISnackbar Snackbar

<MudButton StartIcon="@Icons.Material.Filled.Share"
           OnClick="SharePage">
    Share
</MudButton>

@code {
    private async Task SharePage()
    {
        var currentUrl = Navigation.Uri;

        try
        {
            await Clipboard.WriteTextAsync(currentUrl);
            Snackbar.Add("Link copied to clipboard", Severity.Success);
        }
        catch
        {
            Snackbar.Add("Failed to copy link", Severity.Error);
        }
    }
}
```

---

### Copy JSON Response

```razor
@inject ClipboardService Clipboard
@inject ISnackbar Snackbar

<MudPaper Class="pa-4">
    <MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
        <MudText Typo="Typo.h6">API Response</MudText>
        <MudButton StartIcon="@Icons.Material.Filled.ContentCopy"
                   Size="Size.Small"
                   OnClick="CopyResponse">
            Copy JSON
        </MudButton>
    </MudStack>

    <pre class="mt-4"><code>@_jsonResponse</code></pre>
</MudPaper>

@code {
    private string _jsonResponse = @"{
  ""id"": 123,
  ""name"": ""John Doe"",
  ""email"": ""john@example.com""
}";

    private async Task CopyResponse()
    {
        await Clipboard.WriteTextAsync(_jsonResponse);
        Snackbar.Add("JSON copied to clipboard", Severity.Success);
    }
}
```

---

### Copy with Formatting

```razor
@inject ClipboardService Clipboard

<MudButton OnClick="CopyFormattedText">Copy as Markdown</MudButton>

@code {
    private List<User> _users = new()
    {
        new User { Name = "Alice", Email = "alice@test.com", Role = "Admin" },
        new User { Name = "Bob", Email = "bob@test.com", Role = "User" }
    };

    private async Task CopyFormattedText()
    {
        // Format as Markdown table
        var markdown = new StringBuilder();
        markdown.AppendLine("| Name | Email | Role |");
        markdown.AppendLine("|------|-------|------|");

        foreach (var user in _users)
        {
            markdown.AppendLine($"| {user.Name} | {user.Email} | {user.Role} |");
        }

        await Clipboard.WriteTextAsync(markdown.ToString());
    }
}
```

---

### Copy Multiple Items (Sequential)

```razor
@inject ClipboardService Clipboard
@inject ISnackbar Snackbar

<MudButton OnClick="CopyAllIds">Copy All IDs</MudButton>

@code {
    private List<int> _selectedIds = new() { 123, 456, 789 };

    private async Task CopyAllIds()
    {
        // Copy as comma-separated list
        var ids = string.Join(", ", _selectedIds);

        await Clipboard.WriteTextAsync(ids);
        Snackbar.Add($"Copied {_selectedIds.Count} IDs", Severity.Success);
    }
}
```

---

### Paste and Parse CSV

```razor
@inject ClipboardService Clipboard
@inject ISnackbar Snackbar

<MudButton OnClick="PasteCsv">Paste CSV Data</MudButton>

<MudTable Items="_parsedData">
    <HeaderContent>
        <MudTh>Column 1</MudTh>
        <MudTh>Column 2</MudTh>
        <MudTh>Column 3</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>@context[0]</MudTd>
        <MudTd>@context[1]</MudTd>
        <MudTd>@context[2]</MudTd>
    </RowTemplate>
</MudTable>

@code {
    private List<string[]> _parsedData = new();

    private async Task PasteCsv()
    {
        try
        {
            var csv = await Clipboard.ReadTextAsync();

            // Simple CSV parsing (use proper CSV library for production)
            _parsedData = csv
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split(','))
                .ToList();

            Snackbar.Add($"Parsed {_parsedData.Count} rows", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to parse CSV: {ex.Message}", Severity.Error);
        }
    }
}
```

---

## Security Considerations

### HTTPS Required

The Clipboard API requires a secure context (HTTPS) in production.

**Development:** Works on `localhost` over HTTP
**Production:** Requires HTTPS

```
✓ https://myapp.com (Clipboard works)
✓ http://localhost:5000 (Clipboard works)
✗ http://myapp.com (Clipboard fails)
```

---

### User Permission

Reading from clipboard may require user permission in some browsers.

**Writing:** Usually doesn't require permission
**Reading:** Often requires user interaction or permission

**Best Practice:**

```csharp
private async Task ReadClipboard()
{
    try
    {
        var text = await Clipboard.ReadTextAsync();
        // Use text
    }
    catch (JSException ex)
    {
        // Permission denied or not supported
        Snackbar.Add("Clipboard access denied. Please allow clipboard access in browser settings.",
            Severity.Warning);
    }
}
```

---

### Sensitive Data

Avoid copying sensitive data to clipboard unnecessarily.

**Bad:**

```csharp
// Don't copy passwords, credit cards, etc. automatically
await Clipboard.WriteTextAsync(user.Password);
```

**Good:**

```csharp
// Copy only when user explicitly requests
<MudButton OnClick="() => Clipboard.WriteTextAsync(apiKey)">
    Copy API Key
</MudButton>
```

---

### Clear Clipboard

Browser clipboard persists. Consider clearing sensitive data:

```csharp
private async Task CopyTemporaryToken()
{
    var token = GenerateTemporaryToken();

    await Clipboard.WriteTextAsync(token);
    Snackbar.Add("Token copied (valid for 30 seconds)", Severity.Info);

    // Clear after 30 seconds
    _ = Task.Run(async () =>
    {
        await Task.Delay(30000);
        await Clipboard.WriteTextAsync(""); // Clear clipboard
    });
}
```

---

## Browser Compatibility

### Supported Browsers

- Chrome 66+
- Edge 79+
- Firefox 63+
- Safari 13.1+
- Opera 53+

### Unsupported Browsers

Older browsers don't support Clipboard API. Handle gracefully:

```csharp
private async Task CopyText(string text)
{
    try
    {
        await Clipboard.WriteTextAsync(text);
        Snackbar.Add("Copied to clipboard", Severity.Success);
    }
    catch (JSException)
    {
        // Fallback for unsupported browsers
        Snackbar.Add($"Copy this: {text}", Severity.Info);
    }
}
```

---

### Feature Detection

Check if Clipboard API is available:

```javascript
// wwwroot/js/site.js
window.isClipboardSupported = function() {
    return !!navigator.clipboard;
};
```

```csharp
@inject IJSRuntime JS

private bool _clipboardSupported;

protected override async Task OnInitializedAsync()
{
    _clipboardSupported = await JS.InvokeAsync<bool>("isClipboardSupported");
}

// Conditionally show copy button
@if (_clipboardSupported)
{
    <MudButton OnClick="CopyText">Copy</MudButton>
}
```

---

## Troubleshooting

### "Clipboard access denied"

**Issue:** `JSException` when reading/writing clipboard.

**Causes:**
- Not HTTPS in production
- User denied permission
- Browser doesn't support Clipboard API

**Solutions:**

```csharp
private async Task CopyTextSafe(string text)
{
    try
    {
        await Clipboard.WriteTextAsync(text);
        Snackbar.Add("Copied!", Severity.Success);
    }
    catch (JSException ex)
    {
        Debug.WriteLine($"Clipboard error: {ex.Message}");

        // Fallback: Show text in dialog
        await DialogService.ShowMessageBox(
            "Copy This",
            text,
            yesText: "OK");
    }
}
```

---

### Permission Prompt Not Showing

**Issue:** Browser doesn't prompt for permission.

**Cause:** Clipboard read must be triggered by user interaction.

**Solution:** Ensure read is in response to button click or user action:

```csharp
// ✓ Good - triggered by button click
<MudButton OnClick="PasteText">Paste</MudButton>

// ✗ Bad - automatic on page load
protected override async Task OnInitializedAsync()
{
    await Clipboard.ReadTextAsync(); // Won't work
}
```

---

### Empty String Returned

**Issue:** `ReadTextAsync()` returns empty string.

**Cause:** Clipboard is actually empty or contains non-text data.

**Solution:** Check for empty and handle:

```csharp
private async Task PasteText()
{
    var text = await Clipboard.ReadTextAsync();

    if (string.IsNullOrWhiteSpace(text))
    {
        Snackbar.Add("Clipboard is empty or contains non-text data",
            Severity.Warning);
        return;
    }

    // Use text
}
```

---

### Works in Development, Fails in Production

**Issue:** Works on `localhost`, fails on production domain.

**Cause:** Production is not using HTTPS.

**Solution:** Enable HTTPS in production:

```csharp
// Program.cs
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
```

Or configure web server (IIS, nginx, etc.) for HTTPS.

---

## Advanced Patterns

### Copy with Fallback

Provide fallback for unsupported browsers:

```razor
@inject ClipboardService Clipboard
@inject IDialogService DialogService

<MudButton OnClick="CopyWithFallback">Copy</MudButton>

@code {
    private async Task CopyWithFallback(string text)
    {
        try
        {
            await Clipboard.WriteTextAsync(text);
            Snackbar.Add("Copied!", Severity.Success);
        }
        catch
        {
            // Fallback: Show text in copyable dialog
            var parameters = new DialogParameters
            {
                ["Text"] = text
            };

            await DialogService.Show<CopyFallbackDialog>("Copy This", parameters)
                .Result;
        }
    }
}
```

**CopyFallbackDialog.razor:**

```razor
<MudDialog>
    <DialogContent>
        <MudTextField @bind-Value="_text"
                      Label="Copy this text"
                      Lines="5"
                      ReadOnly="true"
                      Variant="Variant.Outlined" />
    </DialogContent>
    <DialogActions>
        <MudButton Color="Color.Primary" OnClick="Close">Close</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [Parameter] public string Text { get; set; } = "";
    [CascadingParameter] MudDialogInstance MudDialog { get; set; } = null!;

    private string _text => Text;

    private void Close() => MudDialog.Close();
}
```

---

### Debounced Copy

Prevent rapid copying:

```csharp
private DateTime _lastCopy = DateTime.MinValue;
private const int COPY_DEBOUNCE_MS = 1000;

private async Task DebouncedCopy(string text)
{
    var now = DateTime.Now;
    if ((now - _lastCopy).TotalMilliseconds < COPY_DEBOUNCE_MS)
    {
        return; // Too soon
    }

    _lastCopy = now;
    await Clipboard.WriteTextAsync(text);
}
```

---

### Copy History

Track copy history:

```csharp
private Queue<string> _copyHistory = new(10); // Keep last 10

private async Task CopyWithHistory(string text)
{
    await Clipboard.WriteTextAsync(text);

    _copyHistory.Enqueue(text);
    if (_copyHistory.Count > 10)
    {
        _copyHistory.Dequeue(); // Remove oldest
    }
}
```

---

## See Also

- [DownloadHelper.md](DownloadHelper.md) - File download operations
- [Components.md](Components.md) - Blazor UI components
- [Hybrid.md](Hybrid.md) - Blazor Hybrid features
