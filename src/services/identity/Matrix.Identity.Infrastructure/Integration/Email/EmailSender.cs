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

        public Task SendUsernameChanged(
            string toEmail,
            string previousUsername,
            string newUsername,
            CancellationToken cancellationToken)
        {
            return SendAsync(
                toEmail: toEmail,
                subject: "Your username was changed",
                htmlBody:
                $"""
                 <p>Your Matrix account username was changed.</p>
                 <p><strong>Previous username:</strong> {WebUtility.HtmlEncode(previousUsername)}</p>
                 <p><strong>New username:</strong> {WebUtility.HtmlEncode(newUsername)}</p>
                 <p>If you did not make this change, review your account security immediately.</p>
                 """,
                plainBody:
                $"Your Matrix account username was changed.{Environment.NewLine}Previous username: {previousUsername}{Environment.NewLine}New username: {newUsername}{Environment.NewLine}{Environment.NewLine}If you did not make this change, review your account security immediately.",
                linkForLogging: $"username:{previousUsername}->{newUsername}",
                cancellationToken: cancellationToken);
        }

        public Task SendEmailChangeConfirmation(
            string toEmail,
            string currentEmail,
            string confirmationLink,
            CancellationToken cancellationToken)
        {
            return SendAsync(
                toEmail: toEmail,
                subject: "Confirm your new email",
                htmlBody:
                $"""
                 <p>We received a request to replace the email on your Matrix account.</p>
                 <p><strong>Current email:</strong> {WebUtility.HtmlEncode(currentEmail)}</p>
                 <p><strong>New email:</strong> {WebUtility.HtmlEncode(toEmail)}</p>
                 <p><a href="{confirmationLink}">Confirm new email</a></p>
                 <p>If you did not request this change, you can ignore this message.</p>
                 """,
                plainBody:
                $"We received a request to replace the email on your Matrix account.{Environment.NewLine}Current email: {currentEmail}{Environment.NewLine}New email: {toEmail}{Environment.NewLine}{Environment.NewLine}Confirm the new email by opening this link: {confirmationLink}{Environment.NewLine}{Environment.NewLine}If you did not request this change, you can ignore this message.",
                linkForLogging: confirmationLink,
                cancellationToken: cancellationToken);
        }

        public Task SendAccountDeleted(
            string toEmail,
            CancellationToken cancellationToken)
        {
            return SendAsync(
                toEmail: toEmail,
                subject: "Your account was deleted",
                htmlBody:
                """
                <p>Your Matrix account was soft-deleted.</p>
                <p>Sign-in is now disabled and active sessions were revoked.</p>
                <p>If you need the account restored later, contact support or an administrator.</p>
                """,
                plainBody:
                $"Your Matrix account was soft-deleted.{Environment.NewLine}Sign-in is now disabled and active sessions were revoked.{Environment.NewLine}{Environment.NewLine}If you need the account restored later, contact support or an administrator.",
                linkForLogging: "account-deleted",
                cancellationToken: cancellationToken);
        }

        public Task SendAccountRestored(
            string toEmail,
            CancellationToken cancellationToken)
        {
            return SendAsync(
                toEmail: toEmail,
                subject: "Your account was restored",
                htmlBody:
                """
                <p>Your Matrix account was restored by an administrator.</p>
                <p>You can sign in again with your existing credentials unless the account remains separately locked for an administrative reason.</p>
                """,
                plainBody:
                $"Your Matrix account was restored by an administrator.{Environment.NewLine}You can sign in again with your existing credentials unless the account remains separately locked for an administrative reason.",
                linkForLogging: "account-restored",
                cancellationToken: cancellationToken);
        }

        public Task SendAccountRecovery(
            string toEmail,
            string recoveryLink,
            CancellationToken cancellationToken)
        {
            return SendAsync(
                toEmail: toEmail,
                subject: "Restore your account",
                htmlBody:
                $"""
                 <p>We received a request to restore your soft-deleted Matrix account.</p>
                 <p><a href="{recoveryLink}">Restore account</a></p>
                 <p>If you did not request this, you can ignore this message.</p>
                 """,
                plainBody:
                $"We received a request to restore your soft-deleted Matrix account.{Environment.NewLine}Restore the account by opening this link: {recoveryLink}{Environment.NewLine}{Environment.NewLine}If you did not request this, you can ignore this message.",
                linkForLogging: recoveryLink,
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
