namespace Matrix.Identity.Application.Abstractions.Services
{
    public interface IEmailSender
    {
        Task SendEmailConfirmation(
            string toEmail,
            string confirmationLink,
            CancellationToken cancellationToken);

        Task SendPasswordReset(
            string toEmail,
            string resetLink,
            CancellationToken cancellationToken);

        Task SendUsernameChanged(
            string toEmail,
            string previousUsername,
            string newUsername,
            CancellationToken cancellationToken);

        Task SendEmailChangeConfirmation(
            string toEmail,
            string currentEmail,
            string confirmationLink,
            CancellationToken cancellationToken);

        Task SendAccountDeleted(
            string toEmail,
            CancellationToken cancellationToken);

        Task SendAccountRestored(
            string toEmail,
            CancellationToken cancellationToken);

        Task SendAccountRecovery(
            string toEmail,
            string recoveryLink,
            CancellationToken cancellationToken);
    }
}
