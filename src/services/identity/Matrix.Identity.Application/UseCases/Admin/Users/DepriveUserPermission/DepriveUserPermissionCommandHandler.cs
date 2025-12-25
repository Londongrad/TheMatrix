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
            Task<bool> existsTask = userRepository.ExistsAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            Task<PermissionCatalogItemResult?> permTask =
                permissionReadRepository.GetPermissionAsync(
                    permissionKey: request.PermissionKey,
                    cancellationToken: cancellationToken);

            await Task.WhenAll(
                existsTask,
                permTask);

            if (!await existsTask)
                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            PermissionCatalogItemResult? permission = await permTask;

            if (permission is null)
                throw ApplicationErrorsFactory.PermissionNotFound(request.PermissionKey);

            if (permission.IsDeprecated)
                throw ApplicationErrorsFactory.PermissionDeprecated(request.PermissionKey);

            bool changed = await permissionsRepository.UpsertUserPermissionAsync(
                userId: request.UserId,
                permissionKey: request.PermissionKey,
                effect: PermissionEffect.Deny,
                cancellationToken: cancellationToken);

            if (!changed)
                return; // уже Deny — не bumpаем версию, не сохраняем лишний раз

            await userRepository.BumpPermissionsVersionAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
