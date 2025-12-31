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
            bool userExists = await userRepository.GetByIdAsync(
                                  userId: request.UserId,
                                  cancellationToken: cancellationToken) != null;

            if (!userExists)
                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            return await userRolesRepository.GetUserRolesAsync(
                userId: request.UserId,
                ct: cancellationToken);
        }
    }
}
