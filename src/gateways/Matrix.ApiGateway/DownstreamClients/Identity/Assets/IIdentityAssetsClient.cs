namespace Matrix.ApiGateway.DownstreamClients.Identity.Assets
{
    public interface IIdentityAssetsClient
    {
        Task<HttpResponseMessage> GetAvatarAsync(
            string fileName,
            CancellationToken cancellationToken);
    }
}
