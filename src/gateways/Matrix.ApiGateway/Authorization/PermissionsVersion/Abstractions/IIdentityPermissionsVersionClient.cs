namespace Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions
{
    public interface IIdentityPermissionsVersionClient
    {
        Task<int> GetPermissionsVersionAsync(Guid userId, CancellationToken cancellationToken);
    }
}
