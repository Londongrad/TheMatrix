namespace Matrix.ApiGateway.DownstreamClients.Identity.Internal.PermissionsVersion
{
    public interface IIdentityPermissionsVersionClient
    {
        Task<int> GetPermissionsVersionAsync(
            Guid userId,
            CancellationToken cancellationToken);
    }
}
