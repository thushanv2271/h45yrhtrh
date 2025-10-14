using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Branches;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Branches.Delete;
internal sealed class DeleteBranchCommandHandler(IApplicationDbContext context)
    : ICommandHandler<DeleteBranchCommand>
{
    public async Task<Result> Handle(DeleteBranchCommand command, CancellationToken cancellationToken)
    {
        Branch? branch = await context.Branches
            .FirstOrDefaultAsync(b => b.Id == command.BranchId, cancellationToken);

        if (branch is null)
        {
            return Result.Failure(BranchErrors.NotFound(command.BranchId));
        }

        //Check if any users are assigned to this branch
        bool hasUsers = await context.Users
            .AnyAsync(u => u.BranchId == command.BranchId, cancellationToken);

        if (hasUsers)
        {
            return Result.Failure(BranchErrors.HasAssignedUsers);
        }
        context.Branches.Remove(branch);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
