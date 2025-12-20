namespace Matrix.Identity.Application.Abstractions.Services.Authorization
{
    public interface IEffectivePermissionsService
    {
        Task<IReadOnlyCollection<string>> GetPermissionsAsync(
            Guid userId,
            CancellationToken ct);

        Task<IReadOnlyCollection<string>> GetRolesAsync(
            Guid userId,
            CancellationToken ct);

        Task<AuthorizationContext> GetAuthContextAsync(
            Guid userId,
            CancellationToken ct);
    }
}
