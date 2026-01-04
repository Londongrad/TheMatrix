namespace Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions
{
    public interface IPermissionsVersionStore
    {
        Task<int> GetCurrentAsync(Guid userId, CancellationToken cancellationToken);
    }
}
