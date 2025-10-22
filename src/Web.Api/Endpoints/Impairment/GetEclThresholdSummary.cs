using Application.Abstractions.Messaging;
using Application.Impairment.GetEclThresholdSummary;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Impairment;

/// <summary>
/// Endpoint for calculating ECL threshold summary
/// POST /api/impairment/ecl/threshold-summary
/// </summary>
internal sealed class GetEclThresholdSummaryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("impairment/ecl/threshold-summary", async (
            [FromBody] GetEclThresholdSummaryRequest request,
            IQueryHandler<GetEclThresholdSummaryQuery, EclThresholdSummaryResponse> handler,
            CancellationToken cancellationToken) =>
        {
            // Create query from request
            var query = new GetEclThresholdSummaryQuery(
                IndividualSignificantThreshold: request.IndividualSignificantThreshold,
                BranchId: request.BranchId,
                AsOfDate: request.AsOfDate,
                Currency: request.Currency
            );

            // Execute query
            Result<EclThresholdSummaryResponse> result = await handler.Handle(query, cancellationToken);

            // Return response
            return result.Match(
                data => Results.Ok(data),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Impairment")
        .WithName("GetEclThresholdSummary")
        .WithDescription("Calculate ECL threshold summary for individual and collective impairment based on a threshold value")
        .Produces<EclThresholdSummaryResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}

/// <summary>
/// Request model for ECL threshold summary calculation
/// </summary>
public sealed record GetEclThresholdSummaryRequest
{
    /// <summary>
    /// The threshold value to classify individual vs collective impairment
    /// Customers with exposure >= threshold are classified as Individual Impairment
    /// Customers with exposure < threshold are classified as Collective Impairment
    /// </summary>
    public decimal IndividualSignificantThreshold { get; init; }

    /// <summary>
    /// Optional: Filter by specific branch
    /// </summary>
    public Guid? BranchId { get; init; }

    /// <summary>
    /// Optional: The as-of date for the calculation (defaults to latest available date)
    /// </summary>
    public DateOnly? AsOfDate { get; init; }

    /// <summary>
    /// Optional: Filter by currency (e.g., "USD", "EUR")
    /// </summary>
    public string? Currency { get; init; }
}
