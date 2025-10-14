using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Application.Branches.Update;
internal sealed class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator()
    {
        RuleFor(c => c.BranchId).NotEmpty();
        RuleFor(c => c.BranchName).NotEmpty().MaximumLength(150);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(100);
        RuleFor(c => c.ContactNumber).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Address).NotEmpty();
    }
}
