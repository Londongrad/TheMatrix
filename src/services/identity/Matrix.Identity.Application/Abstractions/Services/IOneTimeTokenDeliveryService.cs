namespace Matrix.Identity.Application.Abstractions.Services
{
    public interface IOneTimeTokenDeliveryService
    {
        Task SendEmailConfirmationAsync(
            string email,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken);

        Task SendPasswordResetAsync(
            string email,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken);

        Task SendAccountRecoveryAsync(
            string email,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken);
    }
}
