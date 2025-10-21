namespace Application.Scenarios.GetAll;

/// <summary>
/// Response containing product category with all its segments and scenarios
/// </summary>
public sealed record ProductCategoryWithScenariosResponse
{
    public Guid ProductCategoryId { get; init; }
    public string ProductCategoryName { get; init; } = string.Empty;
    public List<SegmentWithScenariosResponse> Segments { get; init; } = new();
}

/// <summary>
/// Segment information with its scenarios
/// </summary>
public sealed record SegmentWithScenariosResponse
{
    public Guid SegmentId { get; init; }
    public string SegmentName { get; init; } = string.Empty;
    public List<ScenarioDetailResponse> Scenarios { get; init; } = new();
}

/// <summary>
/// Detailed scenario information
/// </summary>
public sealed record ScenarioDetailResponse
{
    public Guid Id { get; init; }
    public string ScenarioName { get; init; } = string.Empty;
    public decimal Probability { get; init; }
    public bool ContractualCashFlowsEnabled { get; init; }
    public bool LastQuarterCashFlowsEnabled { get; init; }
    public bool OtherCashFlowsEnabled { get; init; }
    public bool CollateralValueEnabled { get; init; }
    public Guid? UploadedFileId { get; init; }
    public UploadedFileInfo? UploadedFile { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Uploaded file information
/// </summary>
public sealed record UploadedFileInfo
{
    public Guid Id { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
    public string StoredFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long Size { get; init; }
    public string Url { get; init; } = string.Empty;
    public Guid UploadedBy { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}
