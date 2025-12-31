using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IUserRolesRepository
    {
        Task<IReadOnlyCollection<UserRoleResult>> GetUserRolesAsync(
            Guid userId,
            CancellationToken ct);

        Task ReplaceUserRolesAsync(
            Guid userId,
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken ct);
    }
}
