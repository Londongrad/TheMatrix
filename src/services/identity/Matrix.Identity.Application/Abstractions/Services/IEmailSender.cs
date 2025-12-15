namespace Matrix.Identity.Application.Abstractions.Services
{
    public interface IEmailSender
    {
        Task SendEmailConfirmation(
            string toEmail,
            string confirmationLink,
            CancellationToken cancellationToken);
    }
}
