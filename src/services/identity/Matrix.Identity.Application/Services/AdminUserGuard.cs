using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.Administration;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Authorization;

namespace Matrix.Identity.Application.Services
{
    public sealed class AdminUserGuard(
        ICurrentUserContext currentUserContext,
        IRoleReadRepository roleReadRepository,
        IUserRolesRepository userRolesRepository)
        : IAdminUserGuard
    {
        public async Task EnsureUserCanBeManagedAsync(
            Guid targetUserId,
            CancellationToken cancellationToken)
        {
            Guid currentUserId = currentUserContext.GetUserIdOrThrow();

            if (targetUserId == currentUserId)
                throw ApplicationErrorsFactory.CannotPerformAdminActionOnSelf();

            IReadOnlyCollection<UseCases.Admin.Users.GetUserRoles.UserRoleResult> targetRoles =
                await userRolesRepository.GetUserRolesAsync(
                    userId: targetUserId,
                    cancellationToken: cancellationToken);

            bool isSuperAdminTarget = targetRoles.Any(
                role => string.Equals(
                    role.Name,
                    SystemRoleNames.SuperAdmin,
                    StringComparison.Ordinal));

            if (isSuperAdminTarget)
                throw ApplicationErrorsFactory.SuperAdminUserIsProtected();
        }

        public async Task EnsureRoleAssignmentIsAllowedAsync(
            IReadOnlyCollection<Guid> desiredRoleIds,
            CancellationToken cancellationToken)
        {
            if (desiredRoleIds.Count == 0)
                return;

            Domain.Entities.Role? superAdminRole = await roleReadRepository.GetByNameAsync(
                roleName: SystemRoleNames.SuperAdmin,
                cancellationToken: cancellationToken);

            if (superAdminRole is null)
                return;

            if (desiredRoleIds.Contains(superAdminRole.Id))
                throw ApplicationErrorsFactory.SuperAdminRoleAssignmentForbidden();
        }
    }
}