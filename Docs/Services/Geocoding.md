# Geocoding Services

Complete geocoding system with support for 4 providers (Mapbox, Azure Maps, Google Maps, PTV Maps) and 3 operations (Forward Geocoding, Reverse Geocoding, and Fuzzy Search/Autocomplete).

## Table of Contents

- [Overview](#overview)
- [Provider Comparison](#provider-comparison)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [Operations](#operations)
- [Blazor Integration](#blazor-integration)
- [Best Practices](#best-practices)

## Overview

The geocoding system provides a unified interface for working with multiple geocoding providers. All providers implement the same `IGeocodingService` interface, making it easy to switch providers or use multiple providers in the same application.

### Supported Operations

1. **Forward Geocoding** - Convert addresses to coordinates
2. **Reverse Geocoding** - Convert coordinates to addresses
3. **Fuzzy Search/Autocomplete** - Search for addresses with partial input

### Supported Providers

- **Mapbox** - Modern API with excellent global coverage
- **Azure Maps** - Microsoft's mapping service with strong European coverage
- **Google Maps** - Industry standard with comprehensive data
- **PTV Maps** - Specialized for logistics and transportation

## Provider Comparison

| Feature | Mapbox | Azure Maps | Google Maps | PTV Maps |
|---------|--------|------------|-------------|----------|
| **Forward Geocoding** | ✅ | ✅ | ✅ | ✅ |
| **Reverse Geocoding** | ✅ | ✅ | ✅ | ✅ |
| **Fuzzy Search** | ✅ | ✅ | ✅ | ✅ |
| **Global Coverage** | Excellent | Excellent | Excellent | Good |
| **European Coverage** | Excellent | Excellent | Excellent | Excellent |
| **Pricing** | Free tier + pay-as-you-go | Free tier + pay-as-you-go | Pay-as-you-go | Pay-as-you-go |
| **Rate Limits** | 100k/month free | 1k/day free | $200 credit/month | Varies |
| **Confidence Score** | ✅ | ✅ | ✅ (inferred) | ✅ |
| **Bounding Box** | ✅ | ✅ | ✅ | ❌ |

### When to Use Each Provider

**Mapbox**
- Modern web applications
- Need detailed street-level data
- Global coverage required
- Budget-conscious projects (generous free tier)

**Azure Maps**
- Already using Azure ecosystem
- Need enterprise-grade SLAs
- Strong European coverage required
- Integration with other Azure services

**Google Maps**
- Need the most comprehensive data
- Industry-standard results
- Already using Google Cloud
- Budget allows for premium service

**PTV Maps**
- Logistics and transportation applications
- Route optimization needed
- Truck routing required
- European-focused applications

## Getting Started

### Installation

The geocoding services are part of the `CheapHelpers.Services` package. Install via NuGet:

```bash
dotnet add package CheapHelpers.Services
```

### Required API Keys

Before using the geocoding services, obtain API keys from the providers:

- **Mapbox**: https://account.mapbox.com/access-tokens/
- **Azure Maps**: https://portal.azure.com/ (Search for "Azure Maps")
- **Google Maps**: https://console.cloud.google.com/apis/credentials (Enable "Geocoding API")
- **PTV Maps**: https://developer.myptv.com/

## Configuration

### ASP.NET Core / Blazor Setup

```csharp
using CheapHelpers.Services.Geocoding.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add geocoding services
builder.Services.AddGeocodingServices(options =>
{
    // Set default provider
    options.DefaultProvider = GeocodingProvider.Mapbox;

    // Configure Mapbox
    options.Mapbox.AccessToken = "your-mapbox-token";
    options.Mapbox.TimeoutSeconds = 30;

    // Configure Azure Maps
    options.AzureMaps.SubscriptionKey = "your-subscription-key";
    options.AzureMaps.ClientId = "your-client-id";
    options.AzureMaps.Endpoint = "https://atlas.microsoft.com/search";

    // Configure Google Maps
    options.GoogleMaps.ApiKey = "your-google-api-key";

    // Configure PTV Maps
    options.PtvMaps.ApiKey = "your-ptv-api-key";
});

var app = builder.Build();
```

### appsettings.json Configuration

```json
{
  "Geocoding": {
    "DefaultProvider": "Mapbox",
    "Mapbox": {
      "AccessToken": "your-mapbox-token",
      "TimeoutSeconds": 30
    },
    "AzureMaps": {
      "SubscriptionKey": "your-subscription-key",
      "ClientId": "your-client-id",
      "Endpoint": "https://atlas.microsoft.com/search",
      "TimeoutSeconds": 30
    },
    "GoogleMaps": {
      "ApiKey": "your-google-api-key",
      "TimeoutSeconds": 30
    },
    "PtvMaps": {
      "ApiKey": "your-ptv-api-key",
      "TimeoutSeconds": 30
    }
  }
}
```

Then configure in Startup:

```csharp
builder.Services.AddGeocodingServices(options =>
{
    var geocodingConfig = builder.Configuration.GetSection("Geocoding");
    options.DefaultProvider = Enum.Parse<GeocodingProvider>(
        geocodingConfig["DefaultProvider"] ?? "Mapbox");

    geocodingConfig.GetSection("Mapbox").Bind(options.Mapbox);
    geocodingConfig.GetSection("AzureMaps").Bind(options.AzureMaps);
    geocodingConfig.GetSection("GoogleMaps").Bind(options.GoogleMaps);
    geocodingConfig.GetSection("PtvMaps").Bind(options.PtvMaps);
});
```

## Usage Examples

### Basic Usage - Default Provider

```csharp
public class AddressService
{
    private readonly IGeocodingService _geocodingService;

    public AddressService(IGeocodingService geocodingService)
    {
        _geocodingService = geocodingService;
    }

    public async Task<GeocodingResult?> FindLocationAsync(string address)
    {
        var result = await _geocodingService.GeocodeAsync(address);

        if (result != null)
        {
            Console.WriteLine($"Address: {result.FormattedAddress}");
            Console.WriteLine($"Coordinates: {result.Coordinate.Latitude}, {result.Coordinate.Longitude}");
            Console.WriteLine($"City: {result.Components.City}");
            Console.WriteLine($"Country: {result.Components.Country}");
        }

        return result;
    }
}
```

### Using Specific Provider

```csharp
public class MultiProviderService
{
    private readonly IGeocodingServiceFactory _factory;

    public MultiProviderService(IGeocodingServiceFactory factory)
    {
        _factory = factory;
    }

    public async Task CompareProvidersAsync(string address)
    {
        // Use Mapbox
        var mapboxService = _factory.GetService(GeocodingProvider.Mapbox);
        var mapboxResult = await mapboxService.GeocodeAsync(address);

        // Use Google Maps
        var googleService = _factory.GetService(GeocodingProvider.GoogleMaps);
        var googleResult = await googleService.GeocodeAsync(address);

        // Compare results...
    }
}
```

## Operations

### 1. Forward Geocoding

Convert an address to geographic coordinates.

```csharp
var result = await _geocodingService.GeocodeAsync(
    "1600 Amphitheatre Parkway, Mountain View, CA",
    new GeocodingOptions
    {
        Language = "en",
        Countries = new[] { "US" }
    });

if (result != null)
{
    Console.WriteLine($"Latitude: {result.Coordinate.Latitude}");
    Console.WriteLine($"Longitude: {result.Coordinate.Longitude}");
}
```

### 2. Reverse Geocoding

Convert coordinates to an address.

```csharp
var result = await _geocodingService.ReverseGeocodeAsync(
    latitude: 37.4224764,
    longitude: -122.0842499,
    new GeocodingOptions
    {
        Language = "en"
    });

if (result != null)
{
    Console.WriteLine($"Address: {result.FormattedAddress}");
    Console.WriteLine($"City: {result.Components.City}");
    Console.WriteLine($"Postal Code: {result.Components.PostalCode}");
}
```

### 3. Fuzzy Search / Autocomplete

Search for addresses with partial input - perfect for search boxes.

```csharp
var results = await _geocodingService.SearchAsync(
    "main st",
    new GeocodingOptions
    {
        Language = "en",
        Countries = new[] { "US", "CA" },
        Limit = 10,
        ProximityBias = new GeoCoordinate(40.7128, -74.0060) // Bias towards NYC
    });

foreach (var result in results)
{
    Console.WriteLine($"{result.FormattedAddress} (Confidence: {result.Confidence:P0})");
}
```

### Advanced Options

```csharp
var options = new GeocodingOptions
{
    // Language for results
    Language = "en",

    // Restrict to specific countries
    Countries = new[] { "US", "CA", "MX" },

    // Maximum results (for search)
    Limit = 5,

    // Bias results towards a location
    ProximityBias = new GeoCoordinate(37.7749, -122.4194), // San Francisco

    // Restrict to bounding box
    BoundingBox = new BoundingBox
    {
        MinLatitude = 37.0,
        MinLongitude = -123.0,
        MaxLatitude = 38.0,
        MaxLongitude = -121.0
    }
};

var results = await _geocodingService.SearchAsync("coffee shop", options);
```

## Blazor Integration

### Address Search Component

```razor
@page "/address-search"
@inject IGeocodingService GeocodingService

<h3>Address Search</h3>

<div class="search-box">
    <input @bind="searchQuery"
           @bind:event="oninput"
           @onkeyup="HandleSearch"
           placeholder="Enter an address..." />
</div>

@if (isSearching)
{
    <p>Searching...</p>
}
else if (results.Any())
{
    <ul class="results-list">
        @foreach (var result in results)
        {
            <li @onclick="() => SelectAddress(result)">
                <strong>@result.FormattedAddress</strong>
                <br />
                <small>
                    @result.Components.City, @result.Components.State @result.Components.PostalCode
                    (@result.Confidence?.ToString("P0") ?? "N/A")
                </small>
            </li>
        }
    </ul>
}

@if (selectedResult != null)
{
    <div class="selected-address">
        <h4>Selected Address</h4>
        <p><strong>Address:</strong> @selectedResult.FormattedAddress</p>
        <p><strong>Coordinates:</strong> @selectedResult.Coordinate.Latitude, @selectedResult.Coordinate.Longitude</p>
        <p><strong>City:</strong> @selectedResult.Components.City</p>
        <p><strong>State:</strong> @selectedResult.Components.State</p>
        <p><strong>Postal Code:</strong> @selectedResult.Components.PostalCode</p>
        <p><strong>Country:</strong> @selectedResult.Components.Country</p>
    </div>
}

@code {
    private string searchQuery = string.Empty;
    private List<GeocodingResult> results = new();
    private GeocodingResult? selectedResult;
    private bool isSearching;
    private System.Threading.Timer? debounceTimer;

    private void HandleSearch()
    {
        // Debounce search
        debounceTimer?.Dispose();
        debounceTimer = new System.Threading.Timer(async _ =>
        {
            await PerformSearch();
        }, null, 300, Timeout.Infinite);
    }

    private async Task PerformSearch()
    {
        if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery.Length < 3)
        {
            results.Clear();
            await InvokeAsync(StateHasChanged);
            return;
        }

        isSearching = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            results = await GeocodingService.SearchAsync(
                searchQuery,
                new GeocodingOptions
                {
                    Limit = 5,
                    Countries = new[] { "US", "CA" }
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Search error: {ex.Message}");
            results.Clear();
        }
        finally
        {
            isSearching = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void SelectAddress(GeocodingResult result)
    {
        selectedResult = result;
        searchQuery = result.FormattedAddress;
        results.Clear();
    }

    public void Dispose()
    {
        debounceTimer?.Dispose();
    }
}
```

### Map Component with Reverse Geocoding

```razor
@page "/reverse-geocode"
@inject IGeocodingService GeocodingService

<h3>Click on Map to Get Address</h3>

<div @onclick="HandleMapClick" style="width: 100%; height: 400px; border: 1px solid #ccc;">
    <!-- Your map component here -->
    <p>Clicked: @clickedLat, @clickedLng</p>
</div>

@if (address != null)
{
    <div class="address-result">
        <h4>Address at Location</h4>
        <p>@address.FormattedAddress</p>
    </div>
}

@code {
    private double? clickedLat;
    private double? clickedLng;
    private GeocodingResult? address;

    private async Task HandleMapClick(MouseEventArgs e)
    {
        // Extract coordinates from click event
        // This is simplified - actual implementation depends on map library
        clickedLat = 37.4224764;
        clickedLng = -122.0842499;

        if (clickedLat.HasValue && clickedLng.HasValue)
        {
            address = await GeocodingService.ReverseGeocodeAsync(
                clickedLat.Value,
                clickedLng.Value);
        }
    }
}
```

## Best Practices

### 1. Provider Selection

- **Use Mapbox** for modern web apps with budget constraints
- **Use Azure Maps** if you're in the Azure ecosystem
- **Use Google Maps** for maximum data coverage and accuracy
- **Use PTV Maps** for logistics and European-focused applications

### 2. Caching

Implement caching to reduce API calls and costs:

```csharp
public class CachedGeocodingService : IGeocodingService
{
    private readonly IGeocodingService _innerService;
    private readonly IMemoryCache _cache;

    public async Task<GeocodingResult?> GeocodeAsync(
        string address,
        GeocodingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"geocode:{address}:{options?.Language}";

        if (_cache.TryGetValue<GeocodingResult>(cacheKey, out var cached))
            return cached;

        var result = await _innerService.GeocodeAsync(address, options, cancellationToken);

        if (result != null)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
        }

        return result;
    }
}
```

### 3. Error Handling

Always handle errors gracefully:

```csharp
try
{
    var result = await _geocodingService.GeocodeAsync(address);

    if (result == null)
    {
        // No results found - show user-friendly message
        return Results.NotFound("Address not found");
    }

    return Results.Ok(result);
}
catch (HttpRequestException ex)
{
    // Network error - retry or show error
    _logger.LogError(ex, "Geocoding API error");
    return Results.Problem("Unable to connect to geocoding service");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected geocoding error");
    return Results.Problem("An unexpected error occurred");
}
```

### 4. Rate Limiting

Be aware of rate limits and implement throttling:

```csharp
public class RateLimitedGeocodingService : IGeocodingService
{
    private readonly IGeocodingService _innerService;
    private readonly SemaphoreSlim _semaphore = new(maxConcurrentRequests: 5);

    public async Task<GeocodingResult?> GeocodeAsync(...)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await _innerService.GeocodeAsync(...);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### 5. Validation

Validate coordinates before reverse geocoding:

```csharp
public async Task<GeocodingResult?> SafeReverseGeocodeAsync(
    double latitude,
    double longitude)
{
    var coordinate = new GeoCoordinate(latitude, longitude);

    if (!coordinate.IsValid())
    {
        throw new ArgumentException("Invalid coordinates");
    }

    return await _geocodingService.ReverseGeocodeAsync(latitude, longitude);
}
```

### 6. Debouncing Search

For autocomplete, implement debouncing to avoid excessive API calls:

```csharp
private CancellationTokenSource? _searchCts;

public async Task SearchWithDebounce(string query)
{
    _searchCts?.Cancel();
    _searchCts = new CancellationTokenSource();

    try
    {
        await Task.Delay(300, _searchCts.Token);
        var results = await _geocodingService.SearchAsync(query, cancellationToken: _searchCts.Token);
        // Update UI
    }
    catch (TaskCanceledException)
    {
        // Search was cancelled - ignore
    }
}
```

## Models Reference

### GeocodingResult

```csharp
public class GeocodingResult
{
    public string FormattedAddress { get; init; }        // Full formatted address
    public GeoCoordinate Coordinate { get; init; }       // Lat/Lng coordinates
    public AddressComponents Components { get; init; }   // Structured address parts
    public double? Confidence { get; init; }             // 0-1 confidence score
    public string? PlaceId { get; init; }                // Provider-specific ID
    public BoundingBox? BoundingBox { get; init; }       // Geographic bounds
    public string Provider { get; init; }                // Provider name
}
```

### AddressComponents

```csharp
public class AddressComponents
{
    public string? StreetNumber { get; init; }
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? District { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? CountryCode { get; init; }
}
```

### GeocodingOptions

```csharp
public class GeocodingOptions
{
    public string? Language { get; init; } = "en";
    public string[]? Countries { get; init; }
    public int? Limit { get; init; } = 5;
    public GeoCoordinate? ProximityBias { get; init; }
    public BoundingBox? BoundingBox { get; init; }
}
```

## Troubleshooting

### No Results Returned

1. Check API key is valid
2. Verify address format is correct
3. Check country restrictions
4. Verify network connectivity

### Low Confidence Scores

1. Use more specific addresses
2. Include city/state/country
3. Check for typos
4. Try different providers

### Rate Limit Errors

1. Implement caching
2. Add request throttling
3. Upgrade to higher tier
4. Switch providers for overflow

## API Documentation Links

- **Mapbox**: https://docs.mapbox.com/api/search/geocoding/
- **Azure Maps**: https://docs.microsoft.com/en-us/rest/api/maps/search
- **Google Maps**: https://developers.google.com/maps/documentation/geocoding
- **PTV Maps**: https://developer.myptv.com/en/documentation/geocoding-api/
