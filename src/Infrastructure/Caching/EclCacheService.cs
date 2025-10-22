using Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Caching;

/// <summary>
/// Implementation of ECL cache service using IMemoryCache
/// Manages cache invalidation for ECL threshold summaries
/// </summary>
internal sealed class EclCacheService(IMemoryCache cache) : IEclCacheService
{
    private const string CacheKeyPrefix = "ecl_threshold_summary_";
    private static readonly HashSet<string> _cacheKeys = new();
    private static readonly object _lock = new();

    public void InvalidateAllThresholdSummaries()
    {
        lock (_lock)
        {
            foreach (var key in _cacheKeys)
            {
                cache.Remove(key);
            }
            _cacheKeys.Clear();
        }
    }

    public void InvalidateThresholdSummary(Guid? branchId, DateOnly? asOfDate, string? currency)
    {
        lock (_lock)
        {
            // Generate pattern to match cache keys
            string pattern = GeneratePattern(branchId, asOfDate, currency);

            // Find and remove matching cache keys
            var keysToRemove = _cacheKeys
                .Where(key => MatchesPattern(key, pattern))
                .ToList();

            foreach (var key in keysToRemove)
            {
                cache.Remove(key);
                _cacheKeys.Remove(key);
            }
        }
    }

    /// <summary>
    /// Registers a cache key for tracking
    /// This should be called when a new cache entry is created
    /// </summary>
    public static void RegisterCacheKey(string cacheKey)
    {
        lock (_lock)
        {
            _cacheKeys.Add(cacheKey);
        }
    }

    private static string GeneratePattern(Guid? branchId, DateOnly? asOfDate, string? currency)
    {
        string asOfDatePart = asOfDate?.ToString("yyyy-MM-dd") ?? "*";
        string branchIdPart = branchId?.ToString() ?? "*";
        string currencyPart = currency ?? "*";

        return $"{CacheKeyPrefix}{asOfDatePart}_{branchIdPart}_{currencyPart}_";
    }

    private static bool MatchesPattern(string cacheKey, string pattern)
    {
        // Simple pattern matching - could be enhanced with regex if needed
        string[] patternParts = pattern.Split('_');
        string[] keyParts = cacheKey.Split('_');

        if (patternParts.Length > keyParts.Length)
        {
            return false;
        }

        for (int i = 0; i < patternParts.Length - 1; i++) // -1 to exclude the trailing underscore part
        {
            if (patternParts[i] == "*")
            {
                continue;
            }

            if (i >= keyParts.Length || patternParts[i] != keyParts[i])
            {
                return false;
            }
        }

        return true;
    }
}
