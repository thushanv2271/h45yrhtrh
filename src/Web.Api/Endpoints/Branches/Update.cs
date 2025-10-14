using Application.Abstractions.Messaging;
using Application.Branches.Update;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Branches;

internal sealed class Update : IEndpoint
{
    public sealed record UpdateBranchRequest(
        string BranchName,
        string Email,
        string ContactNumber,
        string Address,
        bool IsActive
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("branches/{branchId:guid}", async (
            Guid branchId,
            UpdateBranchRequest request,
            ICommandHandler<UpdateBranchCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateBranchCommand(
                branchId,
                request.BranchName,
                request.Email,
                request.ContactNumber,
                request.Address,
                request.IsActive
            );

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok("Branch updated successfully"),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionEdit)
        .WithTags("Branches");
    }
}
