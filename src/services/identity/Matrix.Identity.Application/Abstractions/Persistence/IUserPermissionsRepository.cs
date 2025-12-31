using Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions;
using Matrix.Identity.Domain.Enums;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IUserPermissionsRepository
    {
        Task<IReadOnlyCollection<UserPermissionOverrideResult>> GetUserPermissionsAsync(
            Guid userId,
            CancellationToken ct);

        Task UpsertUserPermissionAsync(
            Guid userId,
            string permissionKey,
            PermissionEffect effect,
            CancellationToken ct);
    }
}
