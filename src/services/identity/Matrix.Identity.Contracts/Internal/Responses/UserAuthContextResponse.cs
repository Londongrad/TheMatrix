namespace Matrix.Identity.Contracts.Internal.Responses
{
    public sealed record UserAuthContextResponse(
        int PermissionsVersion,
        string[] EffectivePermissions);
}
