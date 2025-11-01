using Microsoft.Extensions.Caching.Memory;

namespace CheapHelpers.Caching;

/// <summary>
/// Memory cache with flexible expiration supporting both absolute and sliding expiration strategies.
/// Items can be configured with either or both expiration types.
/// </summary>
/// <typeparam name="T">The type of items to cache.</typeparam>
public class FlexibleExpirationCache<T> : MemoryCacheBase<T>
{
    private readonly TimeSpan? _absoluteExpiration;
    private readonly TimeSpan? _slidingExpiration;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlexibleExpirationCache{T}"/> class with absolute expiration.
    /// </summary>
    /// <param name="cacheName">The name of this cache instance.</param>
    /// <param name="absoluteExpiration">The time span after which items expire (absolute).</param>
    /// <exception cref="ArgumentNullException">Thrown when cacheName is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when absoluteExpiration is zero or negative.</exception>
    public FlexibleExpirationCache(string cacheName, TimeSpan absoluteExpiration)
        : base(cacheName)
    {
        if (absoluteExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(absoluteExpiration), "Absolute expiration must be greater than zero.");

        _absoluteExpiration = absoluteExpiration;
        _slidingExpiration = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FlexibleExpirationCache{T}"/> class with both expiration strategies.
    /// </summary>
    /// <param name="cacheName">The name of this cache instance.</param>
    /// <param name="absoluteExpiration">The time span after which items expire (absolute). Use null for no absolute expiration.</param>
    /// <param name="slidingExpiration">The time span of inactivity after which items expire. Use null for no sliding expiration.</param>
    /// <exception cref="ArgumentNullException">Thrown when cacheName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when both expiration values are null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either expiration value is zero or negative.</exception>
    public FlexibleExpirationCache(string cacheName, TimeSpan? absoluteExpiration, TimeSpan? slidingExpiration)
        : base(cacheName)
    {
        if (!absoluteExpiration.HasValue && !slidingExpiration.HasValue)
            throw new ArgumentException("At least one expiration strategy must be specified.", nameof(absoluteExpiration));

        if (absoluteExpiration.HasValue && absoluteExpiration.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(absoluteExpiration), "Absolute expiration must be greater than zero.");

        if (slidingExpiration.HasValue && slidingExpiration.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(slidingExpiration), "Sliding expiration must be greater than zero.");

        _absoluteExpiration = absoluteExpiration;
        _slidingExpiration = slidingExpiration;
    }

    /// <summary>
    /// Creates cache entry options with the configured expiration strategies.
    /// </summary>
    /// <returns>The configured cache entry options.</returns>
    protected override MemoryCacheEntryOptions CreateCacheEntryOptions()
    {
        var options = new MemoryCacheEntryOptions();

        if (_absoluteExpiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = _absoluteExpiration.Value;

        if (_slidingExpiration.HasValue)
            options.SlidingExpiration = _slidingExpiration.Value;

        return options;
    }
}
