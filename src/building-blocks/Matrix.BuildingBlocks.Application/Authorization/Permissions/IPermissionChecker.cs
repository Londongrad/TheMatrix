namespace Matrix.BuildingBlocks.Application.Authorization.Permissions
{
    public interface IPermissionChecker
    {
        Task<bool> HasAsync(
            Guid userId,
            string permission,
            CancellationToken cancellationToken);

        Task<bool> HasAnyAsync(
            Guid userId,
            IReadOnlyCollection<string> permissions,
            CancellationToken cancellationToken);

        Task<bool> HasAllAsync(
            Guid userId,
            IReadOnlyCollection<string> permissions,
            CancellationToken cancellationToken);
    }
}
