namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IRolePermissionsRepository
    {
        Task<IReadOnlyCollection<string>> GetRolePermissionsAsync(
            Guid roleId,
            CancellationToken cancellationToken);

        Task<bool> ReplaceRolePermissionsAsync(
            Guid roleId,
            IReadOnlyCollection<string> permissionKeys,
            CancellationToken cancellationToken);
    }
}
