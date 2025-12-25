using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IUserRolesRepository
    {
        Task<IReadOnlyCollection<UserRoleResult>> GetUserRolesAsync(
            Guid userId,
            CancellationToken cancellationToken);

        Task<bool> ReplaceUserRolesAsync(
            Guid userId,
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken cancellationToken);
    }
}
