using Matrix.Identity.Application.Abstractions.Services;

namespace Matrix.Identity.Infrastructure.Integration.Email
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailConfirmation(
            string toEmail,
            string confirmationLink,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
