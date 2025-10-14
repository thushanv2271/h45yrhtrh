using Application.Abstractions.Messaging;
using Application.Branches.GetAll;
using Application.Branches.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Branches;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("branches/{branchId:guid}", async (
            Guid branchId,
            IQueryHandler<GetBranchByIdQuery, BranchResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetBranchByIdQuery(branchId);

            Result<BranchResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionRead)
        .WithTags("Branches");
    }
}
