namespace Matrix.ApiGateway.DownstreamClients.Identity.Self.Assets
{
    public interface IIdentityAssetsClient
    {
        Task<HttpResponseMessage> GetAvatarAsync(
            string fileName,
            CancellationToken cancellationToken);
    }
}
