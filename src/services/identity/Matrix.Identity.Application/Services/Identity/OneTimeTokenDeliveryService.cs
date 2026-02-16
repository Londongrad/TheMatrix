using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Application.Services.Identity
{
    public sealed class OneTimeTokenDeliveryService(
        IUserRepository userRepository,
        IOneTimeTokenRepository oneTimeTokenRepository,
        IOneTimeTokenService oneTimeTokenService,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        IFrontendLinkBuilder frontendLinkBuilder,
        IClock clock) : IOneTimeTokenDeliveryService
    {
        public Task SendEmailConfirmationAsync(
            string email,
            CancellationToken cancellationToken)
        {
            return SendAsync(
                email: email,
                purpose: OneTimeTokenPurpose.EmailConfirmation,
                buildLink: frontendLinkBuilder.BuildConfirmEmailLink,
                sendEmail: emailSender.SendEmailConfirmation,
                skipUser: user => user.IsEmailConfirmed,
                cancellationToken: cancellationToken);
        }

        public Task SendPasswordResetAsync(
            string email,
            CancellationToken cancellationToken)
        {
            return SendAsync(
                email: email,
                purpose: OneTimeTokenPurpose.PasswordReset,
                buildLink: frontendLinkBuilder.BuildResetPasswordLink,
                sendEmail: emailSender.SendPasswordReset,
                skipUser: _ => false,
                cancellationToken: cancellationToken);
        }

        private async Task SendAsync(
            string email,
            OneTimeTokenPurpose purpose,
            Func<Guid, string, string> buildLink,
            Func<string, string, CancellationToken, Task> sendEmail,
            Func<User, bool> skipUser,
            CancellationToken cancellationToken)
        {
            var normalizedEmail = Email.Create(email);

            User? user = await userRepository.GetByEmailAsync(
                normalizedEmail: normalizedEmail.Value,
                cancellationToken: cancellationToken);

            if (user is null || skipUser(user))
                return;

            DateTime nowUtc = clock.UtcNow;

            IReadOnlyList<OneTimeToken> activeTokens = await oneTimeTokenRepository.GetActive(
                userId: user.Id,
                purpose: purpose,
                nowUtc: nowUtc,
                cancellationToken: cancellationToken);

            foreach (OneTimeToken activeToken in activeTokens)
                activeToken.Revoke(nowUtc);

            string rawToken = oneTimeTokenService.GenerateRawToken();
            string tokenHash = oneTimeTokenService.HashToken(rawToken);
            DateTime expiresAtUtc = nowUtc.Add(oneTimeTokenService.GetTtl(purpose));

            var token = OneTimeToken.Create(
                userId: user.Id,
                purpose: purpose,
                tokenHash: tokenHash,
                expiresAtUtc: expiresAtUtc,
                createdAtUtc: nowUtc);

            await oneTimeTokenRepository.Add(
                token: token,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            string link = buildLink(
                user.Id,
                rawToken);

            await sendEmail(
                user.Email.Value,
                link,
                cancellationToken);
        }
    }
}
