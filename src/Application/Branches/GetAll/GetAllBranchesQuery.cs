using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Messaging;

namespace Application.Branches.GetAll;
public sealed record GetAllBranchesQuery(Guid? OrganizationId = null)
    : IQuery<List<BranchResponse>>;
