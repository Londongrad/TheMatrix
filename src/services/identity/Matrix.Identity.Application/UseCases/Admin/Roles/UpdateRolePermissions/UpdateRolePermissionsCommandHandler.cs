using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Abstractions.Services.Validation;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.UpdateRolePermissions
{
    public sealed class UpdateRolePermissionsCommandHandler(
        IRoleReadRepository roleReadRepository,
        IRolePermissionsRepository rolePermissionsRepository,
        IPermissionKeysValidator permissionKeysValidator,
        IUserRepository userRepository,
        ISecurityStateChangeCollector securityStateChangeCollector,
        IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateRolePermissionsCommand>
    {
        public async Task Handle(
            UpdateRolePermissionsCommand request,
            CancellationToken cancellationToken)
        {
            Role? role = await roleReadRepository.GetByIdAsync(
                roleId: request.RoleId,
                cancellationToken: cancellationToken);

            if (role is null)
                throw ApplicationErrorsFactory.RoleNotFound(request.RoleId);

            if (role.IsSystem)
                throw ApplicationErrorsFactory.SystemRoleIsReadOnly(role.Name);

            var desiredKeys = request.RolePermissionKeys
               .Where(k => !string.IsNullOrWhiteSpace(k))
               .Select(k => k.Trim())
               .ToHashSet(StringComparer.Ordinal);

            await permissionKeysValidator.ValidateAsync(
                permissionKeys: desiredKeys,
                cancellationToken: cancellationToken);

            await unitOfWork.ExecuteInTransactionAsync(
                action: async token =>
                {
                    bool changed = await rolePermissionsRepository.ReplaceRolePermissionsAsync(
                        roleId: request.RoleId,
                        permissionKeys: desiredKeys,
                        cancellationToken: token);

                    if (!changed)
                        return;

                    IReadOnlyCollection<Guid> affectedUsers = await userRepository.GetUserIdsByRoleAsync(
                        roleId: request.RoleId,
                        cancellationToken: token);

                    foreach (Guid userId in affectedUsers)
                        securityStateChangeCollector.MarkUserChanged(userId);
                },
                cancellationToken: cancellationToken);
        }
    }
}
