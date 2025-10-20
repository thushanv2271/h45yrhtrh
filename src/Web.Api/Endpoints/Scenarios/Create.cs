using Application.Abstractions.Messaging;
using Application.Scenarios.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Scenarios;

internal sealed class Create : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("scenarios", async (
            CreateScenarioRequest request,
            ICommandHandler<CreateScenarioCommand, CreateScenarioResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateScenarioCommand(
                request.ProductCategoryId,
                request.ProductCategoryName,
                request.SegmentId,
                request.SegmentName,
                [.. request.Scenarios.Select(s => new ScenarioItem(  // Simplified collection initialization
                    s.ScenarioName,
                    s.Probability,
                    s.ContractualCashFlowsEnabled,
                    s.LastQuarterCashFlowsEnabled,
                    s.OtherCashFlowsEnabled,
                    s.CollateralValueEnabled,
                    s.UploadFile != null ? new UploadFileItem(
                        s.UploadFile.OriginalFileName,
                        s.UploadFile.StoredFileName,
                        s.UploadFile.ContentType,
                        s.UploadFile.Size,
                        new Uri(s.UploadFile.Url),  // Convert string to Uri
                        s.UploadFile.UploadedBy
                    ) : null
                ))]
            );

            Result<CreateScenarioResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Scenarios");
    }

    public sealed record CreateScenarioRequest(
        Guid ProductCategoryId,
        string ProductCategoryName,
        Guid SegmentId,
        string SegmentName,
        List<ScenarioItemRequest> Scenarios
    );

    public sealed record ScenarioItemRequest(
        string ScenarioName,
        decimal Probability,
        bool ContractualCashFlowsEnabled,
        bool LastQuarterCashFlowsEnabled,
        bool OtherCashFlowsEnabled,
        bool CollateralValueEnabled,
        UploadFileRequest? UploadFile
    );

    public sealed record UploadFileRequest(
        string OriginalFileName,
        string StoredFileName,
        string ContentType,
        long Size,
        string Url,  // Keep as string in request, convert to Uri when mapping
        Guid UploadedBy
    );
}
