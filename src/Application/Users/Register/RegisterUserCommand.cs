using Application.Abstractions.Messaging;

namespace Application.Users.Register;

public sealed record RegisterUserCommand(string Email, string FirstName, string LastName, List<Guid> RoleIds, Guid? BranchId)
    : ICommand<Guid>;
