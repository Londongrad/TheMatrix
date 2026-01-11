namespace Matrix.BuildingBlocks.Application.Authorization.Permissions
{
    public interface IPermissionChecker
    {
        Task<bool> HasAsync(
            Guid userId,
            string permission,
            CancellationToken cancellationToken);
    }
}
