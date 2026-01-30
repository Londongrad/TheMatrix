using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.Administration;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
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
        IAdminUserGuard adminUserGuard,
        ISecurityStateChangeCollector securityStateChangeCollector,
        IUnitOfWork unitOfWork)
        : IRequestHandler<DepriveUserPermissionCommand>
    {
        public async Task Handle(
            DepriveUserPermissionCommand request,
            CancellationToken cancellationToken)
        {
            bool exists = await userRepository.ExistsAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            if (!exists)
                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            await adminUserGuard.EnsureUserCanBeManagedAsync(
                targetUserId: request.UserId,
                cancellationToken: cancellationToken);

            PermissionCatalogItemResult? permission =
                await permissionReadRepository.GetPermissionAsync(
                    permissionKey: request.TargetPermissionKey,
                    cancellationToken: cancellationToken);

            if (permission is null)
                throw ApplicationErrorsFactory.PermissionNotFound(request.TargetPermissionKey);

            if (permission.IsDeprecated)
                throw ApplicationErrorsFactory.PermissionDeprecated(request.TargetPermissionKey);

            await unitOfWork.ExecuteInTransactionAsync(
                action: async token =>
                {
                    bool changed = await permissionsRepository.UpsertUserPermissionAsync(
                        userId: request.UserId,
                        permissionKey: request.TargetPermissionKey,
                        effect: PermissionEffect.Deny,
                        cancellationToken: token);

                    if (!changed)
                        return;

                    securityStateChangeCollector.MarkUserChanged(request.UserId);
                },
                cancellationToken: cancellationToken);
        }
    }
}
