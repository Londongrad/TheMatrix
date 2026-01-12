namespace Matrix.ApiGateway.Authorization.InternalJwt.Abstractions
{
    public interface IInternalJwtIssuer
    {
        string Issue(
            Guid userId,
            string? jti,
            int permissionsVersion,
            IReadOnlyCollection<string> permissions);
    }
}
