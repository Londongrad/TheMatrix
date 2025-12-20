namespace Matrix.Identity.Application.Abstractions.Services.Authorization
{
    public interface IEffectivePermissionsService
    {
        Task<AuthorizationContext> GetAuthContextAsync(
            Guid userId,
            CancellationToken ct);
    }
}
