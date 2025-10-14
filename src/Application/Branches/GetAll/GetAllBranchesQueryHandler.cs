using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Branches.GetAll;
internal sealed class GetAllBranchesQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetAllBranchesQuery, List<BranchResponse>>
{
    public async Task<Result<List<BranchResponse>>> Handle(
        GetAllBranchesQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Domain.Branches.Branch> query = context.Branches.AsQueryable();

        if (request.OrganizationId.HasValue)
        {
            query = query.Where(b => b.OrganizationId == request.OrganizationId.Value);
        }

        List<BranchResponse> branches = await query
            .OrderBy(b => b.BranchName)
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
            .ToListAsync(cancellationToken);

        return Result.Success(branches);
    }
}
