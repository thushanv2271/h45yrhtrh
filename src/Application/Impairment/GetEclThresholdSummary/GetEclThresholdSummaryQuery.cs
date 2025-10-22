using Application.Abstractions.Messaging;

namespace Application.Impairment.GetEclThresholdSummary;

/// <summary>
/// Query to calculate ECL threshold summary for individual and collective impairment
/// </summary>
/// <param name="IndividualSignificantThreshold">The threshold to classify individual vs collective impairment</param>
/// <param name="BranchId">The branch to filter exposures by (optional)</param>
/// <param name="AsOfDate">The date for which to calculate the summary (optional, defaults to latest)</param>
/// <param name="Currency">The currency to filter by (optional, defaults to all)</param>
public sealed record GetEclThresholdSummaryQuery(
    decimal IndividualSignificantThreshold,
    Guid? BranchId = null,
    DateOnly? AsOfDate = null,
    string? Currency = null
) : IQuery<EclThresholdSummaryResponse>;
