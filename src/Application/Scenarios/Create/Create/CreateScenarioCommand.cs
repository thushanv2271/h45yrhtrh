using Application.Abstractions.Messaging;

namespace Application.Scenarios.Create;

public sealed record CreateScenarioCommand(
    Guid ProductCategoryId,
    Guid SegmentId,
    List<ScenarioItem> Scenarios
) : ICommand<CreateScenarioResponse>;

public sealed record ScenarioItem(
    string ScenarioName,
    decimal Probability,
    bool ContractualCashFlowsEnabled,
    bool LastQuarterCashFlowsEnabled,
    bool OtherCashFlowsEnabled,
    bool CollateralValueEnabled,
    UploadFileItem? UploadFile
);

public sealed record UploadFileItem(
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long Size,
    Uri Url, 
    Guid UploadedBy
);
