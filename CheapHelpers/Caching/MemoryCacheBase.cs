using Microsoft.Extensions.Caching.Memory;

namespace CheapHelpers.Caching;

/// <summary>
/// Base class for memory caching implementations with common functionality.
/// </summary>
/// <typeparam name="T">The type of items to cache.</typeparam>
public abstract class MemoryCacheBase<T> : IDisposable
{
    private readonly IMemoryCache _cache;
    private bool _disposed;

    /// <summary>
    /// Gets the name of this cache instance.
    /// </summary>
    protected string CacheName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheBase{T}"/> class.
    /// </summary>
    /// <param name="cacheName">The name of this cache instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when cacheName is null.</exception>
    protected MemoryCacheBase(string cacheName)
    {
        ArgumentNullException.ThrowIfNull(cacheName);

        CacheName = cacheName;
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    /// <summary>
    /// Gets a cached item by key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached item if found; otherwise, default(T).</returns>
    public T? GetCachedItem(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        ThrowIfDisposed();

        return _cache.TryGetValue(key, out T? cachedEntry) ? cachedEntry : default;
    }

    /// <summary>
    /// Gets a cached item by key, or adds it if not found.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Factory function to create the item if not cached.</param>
    /// <returns>The cached or newly created item.</returns>
    public async Task<T> GetOrAddAsync(string key, Func<string, Task<T>> factory)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        ThrowIfDisposed();

        if (_cache.TryGetValue(key, out T? cachedEntry))
            return cachedEntry!;

        T newEntry = await factory(key).ConfigureAwait(false);
        _cache.Set(key, newEntry, CreateCacheEntryOptions());

        return newEntry;
    }

    /// <summary>
    /// Gets a cached item by key, or adds it if not found using a factory with arguments.
    /// </summary>
    /// <typeparam name="TArgs">The type of factory arguments.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Factory function to create the item if not cached.</param>
    /// <param name="factoryArgs">Arguments to pass to the factory function.</param>
    /// <returns>The cached or newly created item.</returns>
    public async Task<T> GetOrAddAsync<TArgs>(string key, Func<string, TArgs, Task<T>> factory, TArgs factoryArgs)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        ThrowIfDisposed();

        if (_cache.TryGetValue(key, out T? cachedEntry))
            return cachedEntry!;

        T newEntry = await factory(key, factoryArgs).ConfigureAwait(false);
        _cache.Set(key, newEntry, CreateCacheEntryOptions());

        return newEntry;
    }

    /// <summary>
    /// Gets a cached item by key, or adds it if not found (synchronous version).
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Factory function to create the item if not cached.</param>
    /// <returns>The cached or newly created item.</returns>
    public T GetOrAdd(string key, Func<string, T> factory)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        ThrowIfDisposed();

        if (_cache.TryGetValue(key, out T? cachedEntry))
            return cachedEntry!;

        T newEntry = factory(key);
        _cache.Set(key, newEntry, CreateCacheEntryOptions());

        return newEntry;
    }

    /// <summary>
    /// Sets or updates a cached item.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="item">The item to cache.</param>
    public void Set(string key, T item)
    {
        ArgumentNullException.ThrowIfNull(key);
        ThrowIfDisposed();

        _cache.Set(key, item, CreateCacheEntryOptions());
    }

    /// <summary>
    /// Removes a cached item.
    /// </summary>
    /// <param name="key">The cache key.</param>
    public void Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        ThrowIfDisposed();

        _cache.Remove(key);
    }

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>True if the key exists; otherwise, false.</returns>
    public bool ContainsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        ThrowIfDisposed();

        return _cache.TryGetValue(key, out _);
    }

    /// <summary>
    /// Tries to get a cached item.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cachedItem">The cached item if found.</param>
    /// <returns>True if the item was found; otherwise, false.</returns>
    public bool TryGet(string key, out T? cachedItem)
    {
        ArgumentNullException.ThrowIfNull(key);
        ThrowIfDisposed();

        return _cache.TryGetValue(key, out cachedItem);
    }

    /// <summary>
    /// Clears all items from the cache.
    /// </summary>
    public void Clear()
    {
        ThrowIfDisposed();

        // MemoryCache doesn't have a Clear method, so we need to dispose and recreate
        if (_cache is MemoryCache memCache)
        {
            memCache.Compact(1.0); // Remove all items
        }
    }

    /// <summary>
    /// Creates cache entry options for new items.
    /// </summary>
    /// <returns>The configured cache entry options.</returns>
    protected abstract MemoryCacheEntryOptions CreateCacheEntryOptions();

    /// <summary>
    /// Throws if the cache has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(CacheName);
    }

    /// <summary>
    /// Disposes the cache.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _cache?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
