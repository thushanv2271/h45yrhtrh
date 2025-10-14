using Application.Abstractions.Messaging;
using Application.Branches.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Branches;

internal sealed class Create : IEndpoint
{
    public sealed record CreateBranchRequest(
        Guid OrganizationId,
        string BranchName,
        string BranchCode,
        string Email,
        string ContactNumber,
        string Address
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("branches", async (
            CreateBranchRequest request,
            ICommandHandler<CreateBranchCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateBranchCommand(
                request.OrganizationId,
                request.BranchName,
                request.BranchCode,
                request.Email,
                request.ContactNumber,
                request.Address
            );

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                branchId => Results.Ok(new { branchId }),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionCreate)
        .WithTags("Branches");
    }
}
