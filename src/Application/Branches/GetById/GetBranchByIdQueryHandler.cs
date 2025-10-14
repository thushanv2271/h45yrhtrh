using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Branches.GetAll;
using Domain.Branches;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Branches.GetById;
internal sealed class GetBranchByIdQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetBranchByIdQuery, BranchResponse>
{
    public async Task<Result<BranchResponse>> Handle(
        GetBranchByIdQuery query,
        CancellationToken cancellationToken)
    {
        BranchResponse? branch = await context.Branches
            .Where(b => b.Id == query.BranchId)
            .Select(b => new BranchResponse
            {
                Id = b.Id,
                OrganizationId = b.OrganizationId,
                BranchName = b.BranchName,
                BranchCode = b.BranchCode,
                Email = b.Email,
                ContactNumber = b.ContactNumber,
                Address = b.Address,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (branch is null)
        {
            return Result.Failure<BranchResponse>(BranchErrors.NotFound(query.BranchId));
        }

        return Result.Success(branch);
    }
}
