using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Messaging;

namespace Application.Branches.Update;
public sealed record UpdateBranchCommand(
    Guid BranchId,
    string BranchName,
    string Email,
    string ContactNumber,
    string Address,
    bool IsActive
) : ICommand;
