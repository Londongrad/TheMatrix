namespace Matrix.Identity.Contracts.Internal.Events
{
    public sealed record UserSecurityStateChangedV1(
        Guid UserId,
        int PermissionsVersion);
}
