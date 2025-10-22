using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SharedKernel;

namespace Application.Impairment.GetEclThresholdSummary;

/// <summary>
/// Handles the ECL threshold summary calculation
/// Aggregates customer exposures and classifies them as individual or collective impairment
/// </summary>
internal sealed class GetEclThresholdSummaryQueryHandler(
    IApplicationDbContext context,
    IMemoryCache cache)
    : IQueryHandler<GetEclThresholdSummaryQuery, EclThresholdSummaryResponse>
{
    private const int CacheExpirationMinutes = 15;

    public async Task<Result<EclThresholdSummaryResponse>> Handle(
        GetEclThresholdSummaryQuery request,
        CancellationToken cancellationToken)
    {
        // Generate cache key based on query parameters
        string cacheKey = GenerateCacheKey(request);

        // Try to get cached result
        if (cache.TryGetValue<EclThresholdSummaryResponse>(cacheKey, out var cachedResult) && cachedResult != null)
        {
            return Result.Success(cachedResult);
        }

        // Determine the AsOfDate to use (latest if not specified)
        DateOnly asOfDate = request.AsOfDate ?? await GetLatestAsOfDateAsync(request.BranchId, request.Currency, cancellationToken);

        // Build the query with filters
        var query = context.CustomerExposures.AsQueryable();

        // Apply filters
        if (request.BranchId.HasValue)
        {
            query = query.Where(e => e.BranchId == request.BranchId.Value);
        }

        if (!string.IsNullOrEmpty(request.Currency))
        {
            query = query.Where(e => e.Currency == request.Currency);
        }

        query = query.Where(e => e.AsOfDate == asOfDate);

        // Step 1: Aggregate exposures per customer (SUM(amortized_cost) GROUP BY customer_id)
        var customerExposures = await query
            .GroupBy(e => e.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                TotalExposure = g.Sum(e => e.AmortizedCost)
            })
            .ToListAsync(cancellationToken);

        // Step 2: Classify into Individual and Collective based on threshold
        var individualImpairments = customerExposures
            .Where(ce => ce.TotalExposure >= request.IndividualSignificantThreshold)
            .ToList();

        var collectiveImpairments = customerExposures
            .Where(ce => ce.TotalExposure < request.IndividualSignificantThreshold)
            .ToList();

        // Step 3: Calculate summary statistics
        var response = new EclThresholdSummaryResponse
        {
            Individual = new ImpairmentSummary
            {
                CustomerCount = individualImpairments.Count,
                AmortizedCost = individualImpairments.Sum(x => x.TotalExposure)
            },
            Collective = new ImpairmentSummary
            {
                CustomerCount = collectiveImpairments.Count,
                AmortizedCost = collectiveImpairments.Sum(x => x.TotalExposure)
            },
            GrandTotal = new ImpairmentSummary
            {
                CustomerCount = customerExposures.Count,
                AmortizedCost = customerExposures.Sum(x => x.TotalExposure)
            }
        };

        // Cache the result
        cache.Set(cacheKey, response, TimeSpan.FromMinutes(CacheExpirationMinutes));

        return Result.Success(response);
    }

    /// <summary>
    /// Gets the latest AsOfDate from the customer exposures table
    /// </summary>
    private async Task<DateOnly> GetLatestAsOfDateAsync(Guid? branchId, string? currency, CancellationToken cancellationToken)
    {
        var query = context.CustomerExposures.AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(e => e.BranchId == branchId.Value);
        }

        if (!string.IsNullOrEmpty(currency))
        {
            query = query.Where(e => e.Currency == currency);
        }

        var latestDate = await query
            .OrderByDescending(e => e.AsOfDate)
            .Select(e => e.AsOfDate)
            .FirstOrDefaultAsync(cancellationToken);

        return latestDate != default ? latestDate : DateOnly.FromDateTime(DateTime.UtcNow);
    }

    /// <summary>
    /// Generates a cache key based on query parameters
    /// Format: ecl_threshold_summary_{asOfDate}_{branchId}_{currency}_{threshold}
    /// </summary>
    private static string GenerateCacheKey(GetEclThresholdSummaryQuery request)
    {
        string asOfDate = request.AsOfDate?.ToString("yyyy-MM-dd") ?? "latest";
        string branchId = request.BranchId?.ToString() ?? "all";
        string currency = request.Currency ?? "all";
        string threshold = request.IndividualSignificantThreshold.ToString("F4");

        return $"ecl_threshold_summary_{asOfDate}_{branchId}_{currency}_{threshold}";
    }
}
