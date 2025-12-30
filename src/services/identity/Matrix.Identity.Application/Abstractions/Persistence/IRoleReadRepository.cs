using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolesList;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IRoleReadRepository
    {
        Task<IReadOnlyCollection<RoleListItemResult>> GetRolesAsync(CancellationToken cancellationToken);

        Task<IReadOnlyCollection<Guid>> GetExistingRoleIdsAsync(
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken cancellationToken);

        Task<bool> ExistsAsync(
            Guid roleId,
            CancellationToken cancellationToken);

        Task<bool> ExistsByNameAsync(
            string roleName,
            CancellationToken cancellationToken);
    }
}
