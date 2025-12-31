using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolesList;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IRoleReadRepository
    {
        Task<IReadOnlyCollection<RoleListItemResult>> GetRolesAsync(CancellationToken ct);

        Task<IReadOnlyCollection<Guid>> GetExistingRoleIdsAsync(
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken ct);
    }
}
