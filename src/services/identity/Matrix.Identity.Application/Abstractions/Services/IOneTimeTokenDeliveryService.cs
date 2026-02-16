namespace Matrix.Identity.Application.Abstractions.Services
{
    public interface IOneTimeTokenDeliveryService
    {
        Task SendEmailConfirmationAsync(
            string email,
            CancellationToken cancellationToken);

        Task SendPasswordResetAsync(
            string email,
            CancellationToken cancellationToken);
    }
}
