using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions
{
    public sealed class GetUserPermissionsQueryHandler(
        IUserRepository userRepository,
        IUserPermissionsRepository permissionsRepository)
        : IRequestHandler<GetUserPermissionsQuery, IReadOnlyCollection<UserPermissionOverrideResult>>
    {
        public async Task<IReadOnlyCollection<UserPermissionOverrideResult>> Handle(
            GetUserPermissionsQuery request,
            CancellationToken cancellationToken)
        {
            bool userExists = await userRepository.GetByIdAsync(
                                  userId: request.UserId,
                                  cancellationToken: cancellationToken) != null;

            if (!userExists)
                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            return await permissionsRepository.GetUserPermissionsAsync(
                userId: request.UserId,
                ct: cancellationToken);
        }
    }
}
