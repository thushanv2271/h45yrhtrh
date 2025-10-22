namespace Application.Impairment.GetEclThresholdSummary;

/// <summary>
/// Response containing ECL threshold summary with individual and collective impairment details
/// </summary>
public sealed class EclThresholdSummaryResponse
{
    public ImpairmentSummary Individual { get; set; } = new();
    public ImpairmentSummary Collective { get; set; } = new();
    public ImpairmentSummary GrandTotal { get; set; } = new();
}

/// <summary>
/// Summary data for an impairment category
/// </summary>
public sealed class ImpairmentSummary
{
    public int CustomerCount { get; set; }
    public decimal AmortizedCost { get; set; }
}
