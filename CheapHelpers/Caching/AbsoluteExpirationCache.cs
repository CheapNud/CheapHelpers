using Microsoft.Extensions.Caching.Memory;

namespace CheapHelpers.Caching;

/// <summary>
/// Memory cache with absolute expiration time.
/// Items expire at a fixed time regardless of access patterns.
/// </summary>
/// <typeparam name="T">The type of items to cache.</typeparam>
public class AbsoluteExpirationCache<T> : MemoryCacheBase<T>
{
    private readonly TimeSpan _absoluteExpiration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbsoluteExpirationCache{T}"/> class.
    /// </summary>
    /// <param name="cacheName">The name of this cache instance.</param>
    /// <param name="absoluteExpiration">The time span after which items expire.</param>
    /// <exception cref="ArgumentNullException">Thrown when cacheName is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when absoluteExpiration is zero or negative.</exception>
    public AbsoluteExpirationCache(string cacheName, TimeSpan absoluteExpiration)
        : base(cacheName)
    {
        if (absoluteExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(absoluteExpiration), "Absolute expiration must be greater than zero.");

        _absoluteExpiration = absoluteExpiration;
    }

    /// <summary>
    /// Creates cache entry options with absolute expiration.
    /// </summary>
    /// <returns>The configured cache entry options.</returns>
    protected override MemoryCacheEntryOptions CreateCacheEntryOptions()
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _absoluteExpiration
        };
    }
}
