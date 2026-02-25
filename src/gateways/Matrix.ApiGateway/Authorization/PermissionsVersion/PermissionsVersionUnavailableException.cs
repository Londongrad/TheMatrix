namespace Matrix.ApiGateway.Authorization.PermissionsVersion
{
    public sealed class PermissionsVersionUnavailableException(
        Guid userId,
        Exception innerException)
        : Exception(
            message: $"Current permissions version is temporarily unavailable for user '{userId}'.",
            innerException: innerException)
    {
        public Guid UserId { get; } = userId;
    }
}
