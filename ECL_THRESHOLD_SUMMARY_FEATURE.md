# ECL Threshold Summary Feature

## Overview

This feature implements the ECL (Expected Credit Loss) Threshold Summary calculation for the Saral Backend system. It calculates Individual and Collective Impairment summaries based on a user-defined Individual Significant Threshold.

## Architecture

The implementation follows Clean Architecture principles with clear separation of concerns:

### 1. Domain Layer (`/src/Domain/CustomerExposures/`)

**CustomerExposure.cs** - Core entity representing customer exposure data
- `Id`: Unique identifier
- `CustomerId`: Customer identifier
- `AmortizedCost`: The amortized cost value for the customer exposure
- `BranchId`: Foreign key to Branch
- `Currency`: Currency code (e.g., USD, EUR)
- `AsOfDate`: The date for which this exposure is calculated
- `CreatedAt`, `UpdatedAt`: Audit timestamps

### 2. Application Layer (`/src/Application/Impairment/GetEclThresholdSummary/`)

#### Files:
- **GetEclThresholdSummaryQuery.cs** - Query request with parameters
- **EclThresholdSummaryResponse.cs** - Response DTOs
- **GetEclThresholdSummaryQueryHandler.cs** - Core business logic
- **GetEclThresholdSummaryQueryValidator.cs** - Input validation

#### Business Logic:
1. **Aggregation**: Groups customer exposures by `customer_id` and sums `amortized_cost`
2. **Classification**:
   - Individual Impairment: `exposure >= threshold`
   - Collective Impairment: `exposure < threshold`
3. **Calculation**: Computes customer counts and total amortized costs
4. **Caching**: Implements intelligent caching based on query parameters

### 3. Infrastructure Layer

**Database Configuration** (`/src/Infrastructure/CustomerExposures/CustomerExposureConfiguration.cs`)
- Entity Framework Core mapping
- Optimized indexes for performance:
  - `IX_CustomerExposures_CustomerId` - For customer grouping
  - `IX_CustomerExposures_BranchId_AsOfDate_Currency` - For filtering
  - `IX_CustomerExposures_Summary_Query` - Composite index for threshold queries

**Caching Service** (`/src/Infrastructure/Caching/`)
- `IEclCacheService` - Cache management interface
- `EclCacheService` - Implementation with pattern-based invalidation
- Cache expiration: 15 minutes
- Cache key format: `ecl_threshold_summary_{asOfDate}_{branchId}_{currency}_{threshold}`

### 4. Web API Layer (`/src/Web.Api/Endpoints/Impairment/`)

**Endpoint**: `POST /api/impairment/ecl/threshold-summary`

## API Usage

### Request

```json
POST /api/impairment/ecl/threshold-summary
Content-Type: application/json
Authorization: Bearer <token>

{
  "individualSignificantThreshold": 2000000.00,
  "branchId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "asOfDate": "2025-10-22",
  "currency": "USD"
}
```

### Request Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `individualSignificantThreshold` | decimal | Yes | Threshold value to classify individual vs collective impairment |
| `branchId` | Guid | No | Filter by specific branch (optional) |
| `asOfDate` | DateOnly | No | The as-of date for calculation (defaults to latest available) |
| `currency` | string | No | Filter by currency code (optional, max 10 chars) |

### Response

```json
{
  "individual": {
    "customerCount": 1390,
    "amortizedCost": 3000309182.83
  },
  "collective": {
    "customerCount": 1063,
    "amortizedCost": 667330728.02
  },
  "grandTotal": {
    "customerCount": 2453,
    "amortizedCost": 3667639910.85
  }
}
```

### Response Structure

| Field | Type | Description |
|-------|------|-------------|
| `individual` | Object | Summary for individual impairment |
| `individual.customerCount` | int | Number of customers with exposure >= threshold |
| `individual.amortizedCost` | decimal | Total amortized cost for individual impairment |
| `collective` | Object | Summary for collective impairment |
| `collective.customerCount` | int | Number of customers with exposure < threshold |
| `collective.amortizedCost` | decimal | Total amortized cost for collective impairment |
| `grandTotal` | Object | Combined summary |
| `grandTotal.customerCount` | int | Total number of customers |
| `grandTotal.amortizedCost` | decimal | Total amortized cost across all customers |

## Performance Optimizations

### 1. Database Indexes
Three strategic indexes ensure optimal query performance:
- Customer ID index for efficient grouping
- Branch/Date/Currency composite index for filtering
- Full composite index covering the entire query pattern

### 2. Caching Strategy
- **Cache Key**: Based on all query parameters (threshold, branch, date, currency)
- **Expiration**: 15 minutes (configurable)
- **Invalidation**: Pattern-based invalidation when customer exposure data changes
- **Performance Target**: < 3 seconds for > 100k customers ✅

### 3. Query Optimization
- Uses EF Core's compiled queries for repeated execution
- Single database round-trip with efficient grouping
- In-memory classification after aggregation to minimize database load

## Database Migration

To create the database migration for the `CustomerExposure` entity:

```bash
# Navigate to Infrastructure project
cd src/Infrastructure

# Create migration
dotnet ef migrations add AddCustomerExposureEntity --startup-project ../Web.Api

# Apply migration
dotnet ef database update --startup-project ../Web.Api
```

## Testing

### Seeding Sample Data

Use the `CustomerExposureSeeder` to populate test data:

```csharp
// In your database seeding logic
await CustomerExposureSeeder.SeedSampleDataAsync(
    context: dbContext,
    logger: logger,
    branchId: yourBranchId,
    currency: "USD",
    asOfDate: DateOnly.FromDateTime(DateTime.UtcNow)
);
```

This will create:
- 1,390 customers with individual impairment (exposure >= 2M)
- 1,063 customers with collective impairment (exposure < 2M)
- Total: 2,453 customers

### Sample Test Request

```bash
curl -X POST "https://localhost:5001/api/impairment/ecl/threshold-summary" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "individualSignificantThreshold": 2000000.00
  }'
```

## Security

- **Authentication**: Requires valid JWT token
- **Authorization**: Requires `PDSetupAccess` permission
- **Validation**: Input validation via FluentValidation
  - Threshold must be > 0
  - Currency max length: 10 characters

## Cache Management

### Invalidate Cache

```csharp
// Inject IEclCacheService
private readonly IEclCacheService _eclCacheService;

// Invalidate all threshold summaries
_eclCacheService.InvalidateAllThresholdSummaries();

// Invalidate specific cache entries
_eclCacheService.InvalidateThresholdSummary(
    branchId: someBranchId,
    asOfDate: someDate,
    currency: "USD"
);
```

### When to Invalidate

Cache should be invalidated when:
- Customer exposure data is uploaded/updated
- Exposures are recalculated
- Manual refresh is requested by user

## Integration Points

### Recommended Integration Flow

1. **User enters threshold** on "Threshold Analysis" page
2. **Frontend calls** `POST /api/impairment/ecl/threshold-summary`
3. **Backend calculates** or retrieves from cache
4. **Frontend displays** results in summary table

### UI Integration Example

```javascript
async function calculateThresholdSummary(threshold, branchId = null) {
  const response = await fetch('/api/impairment/ecl/threshold-summary', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${accessToken}`
    },
    body: JSON.stringify({
      individualSignificantThreshold: threshold,
      branchId: branchId
    })
  });

  const summary = await response.json();

  // Display in table
  displaySummaryTable(summary);
}
```

## Error Handling

| Error Code | Description | Resolution |
|------------|-------------|------------|
| 400 | Validation Error | Check threshold > 0, currency length |
| 401 | Unauthorized | Provide valid JWT token |
| 403 | Forbidden | User needs PDSetupAccess permission |
| 500 | Server Error | Check logs, verify database connectivity |

## Monitoring

### Key Metrics to Monitor

- **Query execution time**: Should be < 3 seconds for 100k+ customers
- **Cache hit rate**: Higher is better (reduces DB load)
- **Cache invalidation frequency**: Too frequent may indicate data volatility

### Logging

The feature logs:
- Query execution (via Serilog)
- Cache hits/misses
- Sample data seeding operations

## Future Enhancements

Potential improvements:
1. **Distributed Caching**: Use Redis for multi-instance deployments
2. **Background Processing**: Pre-calculate summaries for common thresholds
3. **Historical Tracking**: Store calculated summaries for audit/comparison
4. **Excel Export**: Export summary results to Excel
5. **Configurable Precision**: Allow different decimal precision per currency

## File Structure

```
src/
├── Domain/CustomerExposures/
│   └── CustomerExposure.cs
├── Application/
│   ├── Abstractions/Caching/
│   │   └── IEclCacheService.cs
│   └── Impairment/GetEclThresholdSummary/
│       ├── GetEclThresholdSummaryQuery.cs
│       ├── EclThresholdSummaryResponse.cs
│       ├── GetEclThresholdSummaryQueryHandler.cs
│       └── GetEclThresholdSummaryQueryValidator.cs
├── Infrastructure/
│   ├── Caching/
│   │   └── EclCacheService.cs
│   └── CustomerExposures/
│       ├── CustomerExposureConfiguration.cs
│       └── CustomerExposureSeeder.cs
└── Web.Api/Endpoints/Impairment/
    └── GetEclThresholdSummary.cs
```

## Dependencies

- **Microsoft.Extensions.Caching.Memory**: In-memory caching
- **Entity Framework Core**: Database access and LINQ queries
- **FluentValidation**: Input validation
- **MediatR**: CQRS pattern implementation (via Application abstractions)

## Contact & Support

For questions or issues related to this feature, contact the development team or check the project documentation.
