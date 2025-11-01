using Microsoft.Extensions.Caching.Memory;

namespace CheapHelpers.Caching;

/// <summary>
/// Memory cache with sliding expiration time.
/// Items expire after a period of inactivity. Each access resets the expiration timer.
/// </summary>
/// <typeparam name="T">The type of items to cache.</typeparam>
public class SlidingExpirationCache<T> : MemoryCacheBase<T>
{
    private readonly TimeSpan _slidingExpiration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlidingExpirationCache{T}"/> class.
    /// </summary>
    /// <param name="cacheName">The name of this cache instance.</param>
    /// <param name="slidingExpiration">The time span of inactivity after which items expire.</param>
    /// <exception cref="ArgumentNullException">Thrown when cacheName is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when slidingExpiration is zero or negative.</exception>
    public SlidingExpirationCache(string cacheName, TimeSpan slidingExpiration)
        : base(cacheName)
    {
        if (slidingExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(slidingExpiration), "Sliding expiration must be greater than zero.");

        _slidingExpiration = slidingExpiration;
    }

    /// <summary>
    /// Creates cache entry options with sliding expiration.
    /// </summary>
    /// <returns>The configured cache entry options.</returns>
    protected override MemoryCacheEntryOptions CreateCacheEntryOptions()
    {
        return new MemoryCacheEntryOptions
        {
            SlidingExpiration = _slidingExpiration
        };
    }
}
