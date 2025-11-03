# CheapHelpers.Blazor - Components and UI Utilities

Comprehensive guide to Blazor UI components, base classes, and utilities with MudBlazor integration.

## Table of Contents

- [UI Components](#ui-components)
- [Base Classes](#base-classes)
- [Services](#services)
- [Helpers and Utilities](#helpers-and-utilities)
- [Authentication Pages](#authentication-pages)
- [Configuration](#configuration)

---

## UI Components

### ProgressButton

A MudButton wrapper that shows a loading spinner during async operations.

```razor
<ProgressButton ButtonText="Save"
                OnClick="SaveAsync"
                Color="Color.Primary"
                Variant="Variant.Filled"
                StartIcon="@Icons.Material.Filled.Save" />
```

**Parameters:**
- `ButtonText` - Text to display on the button
- `OnClick` - EventCallback for async operations
- `Color` - MudBlazor color (default: Primary)
- `Variant` - MudBlazor variant (default: Filled)
- `Size` - Button size (default: Medium)
- `Disabled` - Disable button
- `FullWidth` - Make button full width
- `StartIcon` - Icon to display before text

**Features:**
- Automatically shows spinner and "Processing" text during async operations
- Disables button during processing to prevent duplicate submissions
- Fully compatible with MudButton styling and parameters

---

### BarcodeScanner

Camera-based barcode scanner with manual entry fallback.

```razor
<BarcodeScanner OnBarcodeRead="HandleBarcode" />

@code {
    private async Task HandleBarcode(string code)
    {
        // Process scanned barcode
    }
}
```

**Parameters:**
- `OnBarcodeRead` - EventCallback<string> triggered when barcode is scanned

**Features:**
- Uses BlazorZXingJs for barcode detection
- Supports CODE_39 format (configurable)
- Torch/flashlight support for low-light scanning
- Manual entry field as fallback
- Automatic device selection (prefers back camera)
- `Reset()` method to restart scanner

---

### AutoSelector

Autocomplete selector with customizable templates.

**Features:**
- Generic type support for any data model
- Customizable display templates
- Search filtering
- MudBlazor integration

---

### CultureSelector

Language/culture selection component.

```razor
<CultureSelector />
```

**Features:**
- Changes application culture
- Uses CultureController for cookie-based persistence
- Redirects to current page after culture change

---

### ImagePanel & ImageComponent

Image display components with Azure AI Vision integration.

**Features:**
- Image upload and display
- Azure Computer Vision API integration for analysis
- Caption generation
- Object detection
- Dialog-based image viewer

---

### Panel

Reusable panel component for consistent layout.

**Features:**
- Consistent styling across application
- Title and content sections
- Collapsible support

---

### PasswordTextField

Password input field with show/hide toggle.

```razor
<PasswordTextField @bind-Value="password" Label="Password" />
```

**Features:**
- Built-in show/hide password toggle
- MudTextField wrapper
- All MudTextField parameters supported

---

### PinDialog

PIN entry dialog for additional authentication.

**Features:**
- Numeric PIN entry
- Confirmation dialog
- Configurable PIN length

---

### SearchDialog

Global search dialog component.

**Features:**
- Full-text search across configured entities
- Keyboard shortcuts
- Result navigation
- Customizable search providers

---

### SmsDialog

SMS sending dialog component.

**Features:**
- Recipient selection
- Message composition
- Send confirmation
- Error handling

---

### Selector

Generic dropdown selector component.

**Features:**
- Type-safe selection
- Customizable display
- MudSelect wrapper

---

### TranslationText

Component for displaying localized text.

**Features:**
- Automatic localization
- Resource file integration
- Fallback support

---

### UploadFile

File upload component with validation.

**Features:**
- Drag-and-drop support
- File type validation
- Size limits
- Progress tracking
- Multiple file support

---

## Base Classes

### LayoutBase<TUser>

Abstract base class for layout components with user preferences and navigation state.

```csharp
public class MainLayout : LayoutBase<ApplicationUser>
{
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        // Your initialization
    }
}
```

**Features:**
- Automatic user preference loading (dark mode, etc.)
- Navigation state persistence
- User authentication state
- Dispose pattern for batch-saving states

**Properties:**
- `User` - Current authenticated user
- `DarkMode` - Current dark mode preference
- `IsInitialized` - Whether initialization is complete
- `LocalNavigationState` - Dictionary of navigation states

**Methods:**
- `LoadUserPreferencesAsync()` - Load user preferences from database
- `ToggleDarkModeAsync()` - Toggle dark mode and save
- `GetExpandState(key)` - Get navigation expand state
- `SaveNavigationState(key, expanded)` - Save navigation state

**Usage Example:**

```razor
@inherits LayoutBase<ApplicationUser>

<MudThemeProvider Theme="@(DarkMode ? darkTheme : lightTheme)" />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar>
        <MudIconButton Icon="@Icons.Material.Filled.DarkMode"
                       OnClick="ToggleDarkModeAsync" />
    </MudAppBar>
    <MudMainContent>
        @Body
    </MudMainContent>
</MudLayout>
```

---

### NavigationStateBase

Base class for pages that need persistent navigation state.

**Features:**
- Per-page navigation state management
- Automatic state persistence
- User-specific state storage

---

### UserPreferenceBase

Base class for pages that need user preferences.

**Features:**
- User preference loading
- Preference change notifications
- Automatic preference persistence

---

## Services

### UserService

Service for user management and authentication state.

**Methods:**
- `IsAuthenticated(Task<AuthenticationState>)` - Check if user is authenticated
- `GetUserAsync(Task<AuthenticationState>)` - Get current user
- `UpdateUserAsync(user)` - Update user in database
- `UpdateNavigationStateAsync(userId, key, value)` - Update single navigation state
- `UpdateNavigationStatesAsync(userId, states)` - Batch update navigation states

---

### CustomNavigationService

Advanced navigation service with role-based URL parameter encryption.

```csharp
@inject CustomNavigationService NavService

// Navigate with selective encryption
await NavService.NavigateToUrlWithSelectiveEncryptionAsync(
    "/user/profile",
    new Dictionary<string, string>
    {
        { "userId", "123" },
        { "tab", "settings" }
    });

// Get decrypted parameters
var parameters = await NavService.GetDecryptedParametersFromUrlAsync();
var userId = await NavService.GetDecryptedParameterAsync<int>("userId");
```

**Configuration:**

```csharp
services.AddScoped<NavigationEncryptionConfiguration>(sp =>
    new NavigationEncryptionConfiguration
    {
        RoleBasedEncryptionParams = new Dictionary<string, List<string>>
        {
            { "Admin", new List<string> { "userId", "orderId" } },
            { "User", new List<string> { "userId" } }
        },
        CacheDurationMinutes = 10
    });
```

**Features:**
- Role-based parameter encryption
- Automatic encryption/decryption
- Parameter caching for performance
- Type-safe parameter retrieval
- Deterministic encryption for consistent URLs

---

### SearchDialogService

Service for managing global search functionality.

**Features:**
- Dialog-based search
- Custom search providers
- Result navigation

---

### SmsDialogService

Service for SMS sending dialogs.

**Features:**
- Dialog-based SMS composition
- Recipient management
- Send confirmation

---

## Helpers and Utilities

### CookieProvider

Cookie management for Blazor Server.

**Features:**
- Server-side cookie access
- Culture preference persistence
- Session management

---

### Policies

Authorization policy definitions.

```csharp
public static class Policies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireUser = "RequireUser";
}
```

---

### Roles

Role constant definitions.

```csharp
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
```

---

### SearchConfiguration

Configuration for search functionality.

**Properties:**
- `SearchProviders` - List of search providers
- `MaxResults` - Maximum results per provider
- `SearchDelay` - Debounce delay in milliseconds

---

### SearchResult

Model for search results.

**Properties:**
- `Title` - Result title
- `Description` - Result description
- `Url` - Navigation URL
- `Icon` - Display icon
- `Category` - Result category

---

### BaseValidator

FluentValidation base validator with common rules.

```csharp
public class MyModelValidator : BaseValidator<MyModel>
{
    public MyModelValidator()
    {
        RuleFor(x => x.Email).EmailAddress();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
```

**Features:**
- FluentValidation integration
- Common validation rules
- Localization support

---

### DOMRect

Model for JavaScript DOM rectangle measurements.

**Properties:**
- `Left` - Left position
- `Top` - Top position
- `Width` - Width
- `Height` - Height

---

### EncryptedRouteConstraint

Route constraint for encrypted route parameters.

**Features:**
- Automatic parameter decryption
- Invalid parameter rejection
- Security for sensitive URLs

---

### ISmsRecipient & SimplePhoneRecipient

Models for SMS recipient management.

```csharp
public class SimplePhoneRecipient : ISmsRecipient
{
    public string PhoneNumber { get; set; }
    public string Name { get; set; }
}
```

---

### SmsResult & SmsError

Models for SMS sending results.

**Properties:**
- `Success` - Whether send succeeded
- `ErrorMessage` - Error message if failed
- `Recipients` - Recipients list

---

### UploadFileResult

Model for file upload results.

**Properties:**
- `Success` - Whether upload succeeded
- `FilePath` - Saved file path
- `FileName` - Original file name
- `FileSize` - File size in bytes
- `ErrorMessage` - Error message if failed

---

## Authentication Pages

The library includes complete authentication pages compatible with ASP.NET Core Identity:

- **Login** - Standard login with 2FA support
- **Register** - User registration
- **ForgotPassword** - Password reset request
- **ResetPassword** - Password reset confirmation
- **ChangePassword** - Change password for authenticated users
- **SetPassword** - Set initial password
- **ConfirmEmail** - Email confirmation
- **ConfirmEmailChange** - Email change confirmation
- **LoginWith2fa** - Two-factor authentication
- **LoginWithRecoveryCode** - Recovery code login
- **Lockout** - Account lockout page
- **Authenticator** - 2FA setup
- **ResetAuthenticator** - Reset 2FA
- **RecoveryCodes** - View recovery codes
- **SetupPin** - PIN setup for additional security
- **PersonalData** - Personal data management
- **Notifications** - Notification preferences
- **Index** - Account management hub

**Validators:**
- `RegisterValidator` - Registration validation
- `ChangePasswordValidator` - Password change validation
- `ResetPasswordValidator` - Password reset validation
- `SetPasswordValidator` - Initial password validation

---

## Configuration

### CheapHelpersBlazorOptions

Configuration options for the Blazor library.

```csharp
services.AddCheapHelpersBlazor<ApplicationUser>(options =>
{
    options.EnableLocalization = true;
    options.EnableFileDownload = true;
    options.SnackbarPosition = Defaults.Classes.Position.BottomRight;
    options.MaxSnackbars = 5;
    options.SnackbarDuration = 3000;
    options.PreventDuplicateSnackbars = true;
    options.EmailServiceType = typeof(MyEmailService);
});
```

**Properties:**
- `EnableLocalization` - Enable localization support
- `EnableFileDownload` - Enable file download features
- `SnackbarPosition` - Snackbar position
- `MaxSnackbars` - Maximum visible snackbars
- `SnackbarDuration` - Snackbar duration in ms
- `PreventDuplicateSnackbars` - Prevent duplicate snackbars
- `EmailServiceType` - Email service implementation type
- `CustomServices` - Additional custom service registrations

---

### Extension Methods

#### AddCheapHelpersBlazor

Full configuration registration.

```csharp
services.AddCheapHelpersBlazor<ApplicationUser>(options => { });
```

#### AddCheapHelpersBlazorMinimal

Minimal configuration for quick setup.

```csharp
services.AddCheapHelpersBlazorMinimal<ApplicationUser>();
```

#### AddCheapHelpersComplete

Complete setup: CheapContext + Blazor services.

```csharp
services.AddCheapHelpersComplete<ApplicationUser>(
    options => options.UseSqlServer(connectionString));
```

#### AddCheapHelpersCompleteWithIdentity

Everything: CheapContext + Identity + Blazor.

```csharp
services.AddCheapHelpersCompleteWithIdentity<ApplicationUser, IdentityRole>(
    options => options.UseSqlServer(connectionString),
    contextOptions: null,
    configureIdentity: identity =>
    {
        identity.Password.RequireDigit = true;
        identity.Password.RequiredLength = 8;
    },
    configureBlazor: blazor =>
    {
        blazor.EnableLocalization = true;
    });
```

---

## Complete Example

**Program.cs:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Complete setup with Identity
builder.Services.AddCheapHelpersCompleteWithIdentity<ApplicationUser, IdentityRole>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")),
    configureIdentity: identity =>
    {
        identity.Password.RequireDigit = true;
        identity.Password.RequiredLength = 8;
        identity.SignIn.RequireConfirmedEmail = true;
    },
    configureBlazor: blazor =>
    {
        blazor.EnableLocalization = true;
        blazor.EnableFileDownload = true;
        blazor.EmailServiceType = typeof(EmailService);
    });

// Add custom navigation encryption
builder.Services.AddScoped<NavigationEncryptionConfiguration>(sp =>
    new NavigationEncryptionConfiguration
    {
        RoleBasedEncryptionParams = new Dictionary<string, List<string>>
        {
            { "Admin", new List<string> { "userId", "orderId", "customerId" } }
        }
    });

builder.Services.AddScoped<CustomNavigationService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

**MainLayout.razor:**

```razor
@inherits LayoutBase<ApplicationUser>

<MudThemeProvider Theme="@(DarkMode ? darkTheme : lightTheme)" />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1">
        <MudIconButton Icon="@Icons.Material.Filled.Menu"
                       Color="Color.Inherit" />
        <MudSpacer />
        <MudIconButton Icon="@Icons.Material.Filled.DarkMode"
                       Color="Color.Inherit"
                       OnClick="ToggleDarkModeAsync" />
        <LoginDisplay />
    </MudAppBar>

    <MudDrawer Open="true" Elevation="2">
        <MudNavMenu>
            <MudNavLink Href="/" Icon="@Icons.Material.Filled.Home">
                Home
            </MudNavLink>
        </MudNavMenu>
    </MudDrawer>

    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="my-4">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    private MudTheme lightTheme = new MudTheme();
    private MudTheme darkTheme = new MudTheme
    {
        Palette = new PaletteLight
        {
            Primary = Colors.Blue.Default,
            Secondary = Colors.Green.Default,
            AppbarBackground = Colors.Blue.Default
        },
        PaletteDark = new PaletteDark
        {
            Primary = Colors.Blue.Lighten1,
            Black = "#27272f",
            Background = "#1a1a27",
            BackgroundGrey = "#27272f",
            Surface = "#373740",
            TextPrimary = "rgba(255,255,255, 0.87)",
            TextSecondary = "rgba(255,255,255, 0.60)"
        }
    };
}
```

---

## JavaScript Interop

The library includes several JavaScript files in `wwwroot/js/`:

- **boot.js** - Blazor boot configuration
- **infiniteScroll.js** - Infinite scroll functionality
- **pdfThumbnails.js** - PDF thumbnail generation
- **site.js** - General utilities
- **textEditor.js** - Text editor enhancements

Reference in `_Host.cshtml`:

```html
<script src="_content/CheapHelpers.Blazor/js/site.js"></script>
```

---

## See Also

- [Hybrid.md](Hybrid.md) - Blazor Hybrid features (WebView, push notifications)
- [DownloadHelper.md](DownloadHelper.md) - Client-side file downloads
- [ClipboardService.md](ClipboardService.md) - Async clipboard operations
