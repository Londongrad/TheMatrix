namespace Matrix.Identity.Application.Abstractions.Services.Security
{
    public interface ISecurityAuditService
    {
        Task WriteAsync(
            SecurityAuditEntry entry,
            CancellationToken cancellationToken);

        Task<bool> IsLoginAllowedAsync(
            string loginSubject,
            string? ipAddress,
            CancellationToken cancellationToken);

        Task<bool> IsEmailConfirmationRequestAllowedAsync(
            string normalizedEmail,
            string? ipAddress,
            CancellationToken cancellationToken);

        Task<bool> IsPasswordResetRequestAllowedAsync(
            string normalizedEmail,
            string? ipAddress,
            CancellationToken cancellationToken);
    }
}
