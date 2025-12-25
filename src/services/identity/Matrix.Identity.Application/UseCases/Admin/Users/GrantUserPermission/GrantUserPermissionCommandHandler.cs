using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
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
        IUnitOfWork unitOfWork)
        : IRequestHandler<GrantUserPermissionCommand>
    {
        public async Task Handle(
            GrantUserPermissionCommand request,
            CancellationToken cancellationToken)
        {
            Task<bool> existsTask = userRepository.ExistsAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            Task<PermissionCatalogItemResult?> permTask = permissionReadRepository.GetPermissionAsync(
                permissionKey: request.PermissionKey,
                cancellationToken: cancellationToken);

            await Task.WhenAll(
                existsTask,
                permTask);

            // Можно выбрать порядок ошибок:
            // 1) Сначала permission (более “валидаторно”)
            PermissionCatalogItemResult? permission = permTask.Result;

            if (permission is null)
                throw ApplicationErrorsFactory.PermissionNotFound(request.PermissionKey);

            if (permission.IsDeprecated)
                throw ApplicationErrorsFactory.PermissionDeprecated(request.PermissionKey);

            // 2) Потом user existence
            if (!existsTask.Result)
                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            bool changed = await permissionsRepository.UpsertUserPermissionAsync(
                userId: request.UserId,
                permissionKey: request.PermissionKey,
                effect: PermissionEffect.Allow,
                cancellationToken: cancellationToken);

            if (!changed)
                return; // уже Allow -> ничего не меняем, версию не bumpаем

            await userRepository.BumpPermissionsVersionAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
