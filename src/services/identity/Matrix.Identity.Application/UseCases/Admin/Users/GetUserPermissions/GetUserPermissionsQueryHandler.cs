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
            IReadOnlyCollection<UserPermissionOverrideResult> overrides =
                await permissionsRepository.GetUserPermissionsAsync(
                    userId: request.UserId,
                    cancellationToken: cancellationToken);

            if (overrides.Count > 0)
                return overrides;

            bool exists = await userRepository.ExistsAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            if (!exists)
                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            return overrides;
        }
    }
}
