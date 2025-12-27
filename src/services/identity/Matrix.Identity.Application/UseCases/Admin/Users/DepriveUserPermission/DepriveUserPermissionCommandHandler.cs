using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Domain.Enums;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.DepriveUserPermission
{
    public sealed class DepriveUserPermissionCommandHandler(
        IUserRepository userRepository,
        IUserPermissionsRepository permissionsRepository,
        IPermissionReadRepository permissionReadRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<DepriveUserPermissionCommand>
    {
        public async Task Handle(
            DepriveUserPermissionCommand request,
            CancellationToken cancellationToken)
        {
            // 1) user exists
            bool exists = await userRepository.ExistsAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            if (!exists)
                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            // 2) permission validation
            PermissionCatalogItemResult? permission =
                await permissionReadRepository.GetPermissionAsync(
                    permissionKey: request.PermissionKey,
                    cancellationToken: cancellationToken);

            if (permission is null)
                throw ApplicationErrorsFactory.PermissionNotFound(request.PermissionKey);

            if (permission.IsDeprecated)
                throw ApplicationErrorsFactory.PermissionDeprecated(request.PermissionKey);

            // 3) upsert deny
            bool changed = await permissionsRepository.UpsertUserPermissionAsync(
                userId: request.UserId,
                permissionKey: request.PermissionKey,
                effect: PermissionEffect.Deny,
                cancellationToken: cancellationToken);

            if (!changed)
                return;

            await userRepository.BumpPermissionsVersionAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
