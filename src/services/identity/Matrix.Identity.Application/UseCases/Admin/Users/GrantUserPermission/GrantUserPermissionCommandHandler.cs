using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
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
            User user = await userRepository.GetByIdAsync(
                            userId: request.UserId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            var permission = await permissionReadRepository.GetPermissionAsync(
                permissionKey: request.PermissionKey,
                ct: cancellationToken);

            if (permission is null)
                throw ApplicationErrorsFactory.PermissionNotFound(request.PermissionKey);

            if (permission.IsDeprecated)
                throw ApplicationErrorsFactory.PermissionDeprecated(request.PermissionKey);

            await permissionsRepository.UpsertUserPermissionAsync(
                userId: request.UserId,
                permissionKey: request.PermissionKey,
                effect: PermissionEffect.Allow,
                ct: cancellationToken);

            user.BumpPermissionsVersion();

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
