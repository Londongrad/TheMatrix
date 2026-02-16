using Matrix.Identity.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Matrix.Identity.Infrastructure.Integration.Email
{
    public sealed class EmailSender(
        IOptions<EmailOptions> options,
        ILogger<EmailSender> logger) : IEmailSender
    {
        public Task SendEmailConfirmation(
            string toEmail,
            string confirmationLink,
            CancellationToken cancellationToken)
        {
            return SendAsync(
                toEmail: toEmail,
                subject: "Confirm your email",
                htmlBody:
                $"""
                 <p>Confirm your email address to keep your Matrix account secure.</p>
                 <p><a href="{confirmationLink}">Confirm email</a></p>
                 <p>If you did not request this, you can ignore this message.</p>
                 """,
                plainBody:
                $"Confirm your email address by opening this link: {confirmationLink}{Environment.NewLine}{Environment.NewLine}If you did not request this, you can ignore this message.",
                linkForLogging: confirmationLink,
                cancellationToken: cancellationToken);
        }

        public Task SendPasswordReset(
            string toEmail,
            string resetLink,
            CancellationToken cancellationToken)
        {
            return SendAsync(
                toEmail: toEmail,
                subject: "Reset your password",
                htmlBody:
                $"""
                 <p>We received a request to reset your Matrix account password.</p>
                 <p><a href="{resetLink}">Reset password</a></p>
                 <p>If you did not request this, you can ignore this message.</p>
                 """,
                plainBody:
                $"Reset your password by opening this link: {resetLink}{Environment.NewLine}{Environment.NewLine}If you did not request this, you can ignore this message.",
                linkForLogging: resetLink,
                cancellationToken: cancellationToken);
        }

        private async Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            string plainBody,
            string linkForLogging,
            CancellationToken cancellationToken)
        {
            EmailOptions emailOptions = options.Value;

            if (emailOptions.DeliveryMode == EmailDeliveryMode.LogOnly)
            {
                logger.LogInformation(
                    "Identity email in log-only mode. To={ToEmail}; Subject={Subject}; Link={Link}",
                    toEmail,
                    subject,
                    linkForLogging);

                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(emailOptions.FromEmail, emailOptions.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);
            message.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(
                    content: plainBody,
                    contentEncoding: Encoding.UTF8,
                    mediaType: "text/plain"));

            using var client = new SmtpClient(
                host: emailOptions.SmtpHost,
                port: emailOptions.SmtpPort)
            {
                EnableSsl = emailOptions.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(emailOptions.SmtpUsername))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(
                    userName: emailOptions.SmtpUsername,
                    password: emailOptions.SmtpPassword);
            }

            await client.SendMailAsync(message, cancellationToken);
        }
    }
}
