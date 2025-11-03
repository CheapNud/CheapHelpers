# Memory Caching

Thread-safe memory caching implementations with multiple expiration strategies.

## Overview

CheapHelpers provides three specialized memory cache implementations built on top of `Microsoft.Extensions.Caching.Memory`:
- **AbsoluteExpirationCache**: Items expire at a fixed time regardless of access
- **SlidingExpirationCache**: Items expire after a period of inactivity
- **FlexibleExpirationCache**: Supports both absolute and sliding expiration strategies

All implementations inherit from `MemoryCacheBase<T>` which provides common caching operations.

## Namespace

```csharp
using CheapHelpers.Caching;
```

## AbsoluteExpirationCache

Items expire after a fixed duration from the time they were added, regardless of how often they are accessed.

### Constructor

```csharp
public AbsoluteExpirationCache(string cacheName, TimeSpan absoluteExpiration)
```

**Parameters:**
- `cacheName`: Unique identifier for this cache instance
- `absoluteExpiration`: Time span after which items expire

**Throws:**
- `ArgumentNullException`: If cacheName is null
- `ArgumentOutOfRangeException`: If absoluteExpiration is zero or negative

### Example

```csharp
// Create cache with 5-minute absolute expiration
var cache = new AbsoluteExpirationCache<User>("UserCache", TimeSpan.FromMinutes(5));

// Add item - expires in 5 minutes regardless of access
cache.Set("user_123", currentUser);

// Even if accessed repeatedly, still expires after 5 minutes
var user = cache.GetCachedItem("user_123");  // Within 5 min: returns user
Thread.Sleep(TimeSpan.FromMinutes(6));
var expired = cache.GetCachedItem("user_123");  // After 5 min: returns null
```

### Use Cases

- API rate limit tracking (reset after fixed window)
- Session data with fixed expiration
- Temporary tokens or codes
- Daily/hourly data snapshots
- Fixed-window counters

## SlidingExpirationCache

Items expire after a period of inactivity. Each access resets the expiration timer.

### Constructor

```csharp
public SlidingExpirationCache(string cacheName, TimeSpan slidingExpiration)
```

**Parameters:**
- `cacheName`: Unique identifier for this cache instance
- `slidingExpiration`: Time span of inactivity after which items expire

**Throws:**
- `ArgumentNullException`: If cacheName is null
- `ArgumentOutOfRangeException`: If slidingExpiration is zero or negative

### Example

```csharp
// Create cache with 10-minute sliding expiration
var cache = new SlidingExpirationCache<UserPreferences>("PreferencesCache", TimeSpan.FromMinutes(10));

// Add item
cache.Set("prefs_123", userPreferences);

// Access resets the timer
Thread.Sleep(TimeSpan.FromMinutes(8));
var prefs = cache.GetCachedItem("prefs_123");  // Resets expiration timer

Thread.Sleep(TimeSpan.FromMinutes(8));  // Total: 16 minutes elapsed
var stillValid = cache.GetCachedItem("prefs_123");  // Still valid (accessed at 8min mark)

// If not accessed for 10 minutes, expires
Thread.Sleep(TimeSpan.FromMinutes(11));
var expired = cache.GetCachedItem("prefs_123");  // Returns null
```

### Use Cases

- User session data (extend while active)
- Frequently accessed configuration
- Active user preferences
- Connection pools
- Keep-alive scenarios

## FlexibleExpirationCache

Supports both absolute and sliding expiration strategies, either individually or combined.

### Constructors

**Absolute Only:**
```csharp
public FlexibleExpirationCache(string cacheName, TimeSpan absoluteExpiration)
```

**Absolute and/or Sliding:**
```csharp
public FlexibleExpirationCache(
    string cacheName,
    TimeSpan? absoluteExpiration,
    TimeSpan? slidingExpiration)
```

**Parameters:**
- `cacheName`: Unique identifier for this cache instance
- `absoluteExpiration`: Optional fixed expiration time
- `slidingExpiration`: Optional inactivity expiration time

**Throws:**
- `ArgumentNullException`: If cacheName is null
- `ArgumentException`: If both expiration values are null
- `ArgumentOutOfRangeException`: If either expiration value is zero or negative

### Example

```csharp
// Absolute only (equivalent to AbsoluteExpirationCache)
var absoluteCache = new FlexibleExpirationCache<string>(
    "AbsoluteOnly",
    TimeSpan.FromHours(1));

// Sliding only (equivalent to SlidingExpirationCache)
var slidingCache = new FlexibleExpirationCache<string>(
    "SlidingOnly",
    absoluteExpiration: null,
    slidingExpiration: TimeSpan.FromMinutes(30));

// Combined: expires after 1 hour OR 15 minutes of inactivity (whichever comes first)
var combinedCache = new FlexibleExpirationCache<ApiResponse>(
    "ApiCache",
    absoluteExpiration: TimeSpan.FromHours(1),
    slidingExpiration: TimeSpan.FromMinutes(15));

combinedCache.Set("api_response", response);

// Item expires when EITHER:
// 1. 1 hour passes from creation (absolute), OR
// 2. 15 minutes pass without access (sliding)
```

### Use Cases

- Cache entries that should never exceed a maximum lifetime but can expire sooner if unused
- API responses with freshness requirements
- Computed results that are expensive but have known validity periods
- Hybrid caching strategies

## MemoryCacheBase Methods

All cache implementations provide these common methods:

### GetCachedItem

Retrieves a cached item by key.

```csharp
public T? GetCachedItem(string key)
```

**Example:**
```csharp
var user = cache.GetCachedItem("user_123");
if (user == null)
{
    // Cache miss - load from database
    user = await LoadUserFromDatabase(123);
    cache.Set("user_123", user);
}
```

### GetOrAddAsync

Gets a cached item or adds it using an async factory if not found.

```csharp
public async Task<T> GetOrAddAsync(string key, Func<string, Task<T>> factory)
```

**Example:**
```csharp
var user = await cache.GetOrAddAsync("user_123", async key =>
{
    var userId = int.Parse(key.Replace("user_", ""));
    return await userRepository.GetByIdAsync(userId);
});
```

### GetOrAddAsync (with arguments)

Gets a cached item or adds it using a factory with arguments.

```csharp
public async Task<T> GetOrAddAsync<TArgs>(
    string key,
    Func<string, TArgs, Task<T>> factory,
    TArgs factoryArgs)
```

**Example:**
```csharp
var productData = await cache.GetOrAddAsync(
    $"product_{productId}",
    async (key, includeDetails) =>
    {
        return await productService.GetProductAsync(productId, includeDetails);
    },
    includeDetails: true);
```

### GetOrAdd (Synchronous)

Synchronous version of GetOrAdd.

```csharp
public T GetOrAdd(string key, Func<string, T> factory)
```

**Example:**
```csharp
var config = cache.GetOrAdd("app_config", key =>
{
    return ConfigurationLoader.Load();
});
```

### Set

Adds or updates a cached item.

```csharp
public void Set(string key, T item)
```

**Example:**
```csharp
cache.Set("settings_123", userSettings);
```

### Remove

Removes a cached item.

```csharp
public void Remove(string key)
```

**Example:**
```csharp
cache.Remove("user_123");
```

### ContainsKey

Checks if a key exists in the cache.

```csharp
public bool ContainsKey(string key)
```

**Example:**
```csharp
if (cache.ContainsKey("user_123"))
{
    // Item is cached
}
```

### TryGet

Attempts to retrieve a cached item.

```csharp
public bool TryGet(string key, out T? cachedItem)
```

**Example:**
```csharp
if (cache.TryGet("user_123", out var user))
{
    // Cache hit
    return user;
}
else
{
    // Cache miss
    return await LoadFromDatabase();
}
```

### Clear

Removes all items from the cache.

```csharp
public void Clear()
```

**Example:**
```csharp
cache.Clear();  // Empties the entire cache
```

### Dispose

Releases cache resources.

```csharp
public void Dispose()
```

**Example:**
```csharp
using (var cache = new AbsoluteExpirationCache<string>("TempCache", TimeSpan.FromMinutes(5)))
{
    cache.Set("key", "value");
    // Cache is automatically disposed
}
```

## Common Use Cases

### API Response Caching

```csharp
public class WeatherService
{
    private readonly FlexibleExpirationCache<WeatherData> _cache;

    public WeatherService()
    {
        // Expire after 1 hour OR 10 minutes of inactivity
        _cache = new FlexibleExpirationCache<WeatherData>(
            "WeatherCache",
            absoluteExpiration: TimeSpan.FromHours(1),
            slidingExpiration: TimeSpan.FromMinutes(10));
    }

    public async Task<WeatherData> GetWeatherAsync(string city)
    {
        return await _cache.GetOrAddAsync($"weather_{city}", async key =>
        {
            return await externalApi.FetchWeatherAsync(city);
        });
    }
}
```

### User Session Management

```csharp
public class SessionManager
{
    private readonly SlidingExpirationCache<UserSession> _sessions;

    public SessionManager()
    {
        // Sessions expire after 30 minutes of inactivity
        _sessions = new SlidingExpirationCache<UserSession>(
            "Sessions",
            TimeSpan.FromMinutes(30));
    }

    public UserSession GetSession(string sessionId)
    {
        // Each access extends the session
        return _sessions.GetOrAdd(sessionId, key => new UserSession(key));
    }

    public void EndSession(string sessionId)
    {
        _sessions.Remove(sessionId);
    }
}
```

### Computed Results Caching

```csharp
public class ReportService
{
    private readonly AbsoluteExpirationCache<Report> _cache;

    public ReportService()
    {
        // Daily reports cached for 24 hours
        _cache = new AbsoluteExpirationCache<Report>(
            "DailyReports",
            TimeSpan.FromHours(24));
    }

    public async Task<Report> GetDailyReportAsync(DateTime date)
    {
        var cacheKey = $"report_{date:yyyy-MM-dd}";

        return await _cache.GetOrAddAsync(cacheKey, async key =>
        {
            // Expensive computation
            return await GenerateReportAsync(date);
        });
    }
}
```

### Multi-Tier Caching

```csharp
public class ProductService
{
    private readonly SlidingExpirationCache<Product> _hotCache;
    private readonly AbsoluteExpirationCache<Product> _warmCache;

    public ProductService()
    {
        // Hot cache: Frequently accessed items (15 min sliding)
        _hotCache = new SlidingExpirationCache<Product>(
            "HotProducts",
            TimeSpan.FromMinutes(15));

        // Warm cache: All products (1 hour absolute)
        _warmCache = new AbsoluteExpirationCache<Product>(
            "AllProducts",
            TimeSpan.FromHours(1));
    }

    public async Task<Product> GetProductAsync(int productId)
    {
        var key = $"product_{productId}";

        // Try hot cache first
        if (_hotCache.TryGet(key, out var hotProduct))
            return hotProduct;

        // Try warm cache
        var product = await _warmCache.GetOrAddAsync(key, async k =>
        {
            return await database.LoadProductAsync(productId);
        });

        // Promote to hot cache
        _hotCache.Set(key, product);

        return product;
    }
}
```

## Tips and Best Practices

1. **Cache Naming**: Use descriptive cache names for debugging and monitoring. The name appears in error messages and disposal logging.

2. **Expiration Strategy Selection**:
   - Use **Absolute** for time-sensitive data (reports, daily summaries, rate limits)
   - Use **Sliding** for user-specific data (sessions, preferences)
   - Use **Flexible** when you need both guarantees

3. **Memory Management**: Caches use memory. Monitor usage and set appropriate expiration times to prevent memory bloat.

4. **Thread Safety**: All cache implementations are thread-safe. Multiple threads can safely read/write concurrently.

5. **Disposal**: Implement `IDisposable` pattern or use `using` statements to ensure proper cleanup:
   ```csharp
   using var cache = new AbsoluteExpirationCache<T>("Cache", TimeSpan.FromMinutes(5));
   ```

6. **Cache Key Design**: Use consistent, meaningful key naming schemes:
   ```csharp
   $"user_{userId}"
   $"product_{productId}_{includeDetails}"
   $"report_{date:yyyy-MM-dd}_{reportType}"
   ```

7. **GetOrAdd Pattern**: Prefer `GetOrAdd` methods over manual `GetCachedItem` + `Set` to avoid race conditions.

8. **Avoid Caching Nulls**: The cache stores `null` as a valid value. If you need to distinguish between "not cached" and "cached null", use `TryGet`.

9. **Cache Warming**: Pre-populate caches at startup for critical data:
   ```csharp
   public async Task WarmCacheAsync()
   {
       var criticalData = await LoadCriticalDataAsync();
       foreach (var item in criticalData)
       {
           cache.Set($"critical_{item.Id}", item);
       }
   }
   ```

10. **Monitoring**: Log cache hits/misses to optimize expiration times:
    ```csharp
    if (cache.TryGet(key, out var item))
        logger.LogDebug("Cache hit: {Key}", key);
    else
        logger.LogDebug("Cache miss: {Key}", key);
    ```
