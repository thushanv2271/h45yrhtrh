using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Messaging;

namespace Application.Branches.Create;
public sealed record CreateBranchCommand(
    Guid OrganizationId,
    string BranchName,
    string BranchCode,
    string Email,
    string ContactNumber,
    string Address
) : ICommand<Guid>;
