﻿using Application.Abstractions.Messaging;
using Application.Branches.GetAll;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Branches;

internal sealed class GetAll : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("branches", async (
            Guid? organizationId,
            IQueryHandler<GetAllBranchesQuery, List<BranchResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAllBranchesQuery(organizationId);

            Result<List<BranchResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionRead)
        .WithTags("Branches");
    }
}
