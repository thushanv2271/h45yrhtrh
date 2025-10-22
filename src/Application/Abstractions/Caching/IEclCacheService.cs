namespace Application.Abstractions.Caching;

/// <summary>
/// Service for managing ECL threshold summary cache
/// </summary>
public interface IEclCacheService
{
    /// <summary>
    /// Invalidates all ECL threshold summary cache entries
    /// </summary>
    void InvalidateAllThresholdSummaries();

    /// <summary>
    /// Invalidates cache entries for a specific branch and date
    /// </summary>
    void InvalidateThresholdSummary(Guid? branchId, DateOnly? asOfDate, string? currency);
}
