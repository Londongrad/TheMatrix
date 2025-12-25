using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles
{
    public sealed class GetUserRolesQueryHandler(
        IUserRepository userRepository,
        IUserRolesRepository userRolesRepository)
        : IRequestHandler<GetUserRolesQuery, IReadOnlyCollection<UserRoleResult>>
    {
        public async Task<IReadOnlyCollection<UserRoleResult>> Handle(
            GetUserRolesQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<UserRoleResult> roles = await userRolesRepository.GetUserRolesAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            if (roles.Count > 0)
                return roles;

            bool exists = await userRepository.ExistsAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            if (!exists)
                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            return roles;
        }
    }
}
