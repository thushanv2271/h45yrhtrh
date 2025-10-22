using Domain.CustomerExposures;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.CustomerExposures;

/// <summary>
/// Seeds sample customer exposure data for testing ECL threshold summary calculations
/// </summary>
public static class CustomerExposureSeeder
{
    /// <summary>
    /// Seeds sample customer exposure data
    /// Creates a mix of customers with varying exposure amounts to test threshold classification
    /// </summary>
    public static async Task SeedSampleDataAsync(
        ApplicationDbContext context,
        ILogger logger,
        Guid branchId,
        string currency = "USD",
        DateOnly? asOfDate = null)
    {
        asOfDate ??= DateOnly.FromDateTime(DateTime.UtcNow);

        // Check if data already exists
        bool hasData = await context.CustomerExposures
            .AnyAsync(e => e.BranchId == branchId && e.AsOfDate == asOfDate);

        if (hasData)
        {
            logger.LogInformation("Sample customer exposure data already exists for branch {BranchId} and date {AsOfDate}", branchId, asOfDate);
            return;
        }

        logger.LogInformation("Seeding sample customer exposure data for branch {BranchId}...", branchId);

        var random = new Random(42); // Fixed seed for reproducibility
        var exposures = new List<CustomerExposure>();

        // Generate sample data matching the expected output from requirements:
        // Individual Impairment: 1390 customers, 3,000,309,182.83 total
        // Collective Impairment: 1063 customers, 667,330,728.02 total
        // Using threshold around 2,000,000

        // Generate 1390 individual impairment customers (exposure >= 2,000,000)
        for (int i = 1; i <= 1390; i++)
        {
            decimal exposure = 2_000_000m + (decimal)(random.NextDouble() * 5_000_000); // 2M to 7M
            exposures.Add(new CustomerExposure
            {
                Id = Guid.NewGuid(),
                CustomerId = $"CUST-IND-{i:D6}",
                AmortizedCost = Math.Round(exposure, 2),
                BranchId = branchId,
                Currency = currency,
                AsOfDate = asOfDate.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Generate 1063 collective impairment customers (exposure < 2,000,000)
        for (int i = 1; i <= 1063; i++)
        {
            decimal exposure = (decimal)(random.NextDouble() * 1_900_000); // 0 to 1.9M
            exposures.Add(new CustomerExposure
            {
                Id = Guid.NewGuid(),
                CustomerId = $"CUST-COL-{i:D6}",
                AmortizedCost = Math.Round(exposure, 2),
                BranchId = branchId,
                Currency = currency,
                AsOfDate = asOfDate.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Add all exposures to context
        await context.CustomerExposures.AddRangeAsync(exposures);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "Seeded {Count} customer exposures. Individual: {IndividualCount}, Collective: {CollectiveCount}",
            exposures.Count,
            1390,
            1063);
    }
}
