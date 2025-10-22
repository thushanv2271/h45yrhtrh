using Domain.Branches;
using SharedKernel;

namespace Domain.CustomerExposures;

public sealed class CustomerExposure : Entity
{
    public Guid Id { get; set; }

    public string CustomerId { get; set; } = string.Empty;

    public decimal AmortizedCost { get; set; }

    public Guid BranchId { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateOnly AsOfDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Branch? Branch { get; set; }
}
