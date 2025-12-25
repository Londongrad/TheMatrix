using Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions;
using Matrix.Identity.Domain.Enums;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IUserPermissionsRepository
    {
        Task<IReadOnlyCollection<UserPermissionOverrideResult>> GetUserPermissionsAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<bool> UpsertUserPermissionAsync(
            Guid userId,
            string permissionKey,
            PermissionEffect effect,
            CancellationToken cancellationToken);
    }
}
