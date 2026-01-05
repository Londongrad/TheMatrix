using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Domain.Enums;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GrantUserPermission
{
    public sealed class GrantUserPermissionCommandHandler(
        IUserRepository userRepository,
        IUserPermissionsRepository permissionsRepository,
        IPermissionReadRepository permissionReadRepository,
        ISecurityStateChangeCollector securityStateChangeCollector,
        IUnitOfWork unitOfWork)
        : IRequestHandler<GrantUserPermissionCommand>
    {
        public async Task Handle(
            GrantUserPermissionCommand request,
            CancellationToken cancellationToken)
        {
            // 1) user exists
            bool exists = await userRepository.ExistsAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            if (!exists)
                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            // 2) permission
            PermissionCatalogItemResult? permission =
                await permissionReadRepository.GetPermissionAsync(
                    permissionKey: request.PermissionKey,
                    cancellationToken: cancellationToken);

            if (permission is null)
                throw ApplicationErrorsFactory.PermissionNotFound(request.PermissionKey);

            if (permission.IsDeprecated)
                throw ApplicationErrorsFactory.PermissionDeprecated(request.PermissionKey);

            await unitOfWork.ExecuteInTransactionAsync(
                action: async token =>
                {
                    bool changed = await permissionsRepository.UpsertUserPermissionAsync(
                        userId: request.UserId,
                        permissionKey: request.PermissionKey,
                        effect: PermissionEffect.Allow,
                        cancellationToken: token);

                    if (!changed)
                        return; // уже Allow -> ничего не меняем, версию не bumpаем

                    securityStateChangeCollector.MarkUserChanged(request.UserId);
                },
                cancellationToken: cancellationToken);
        }
    }
}
