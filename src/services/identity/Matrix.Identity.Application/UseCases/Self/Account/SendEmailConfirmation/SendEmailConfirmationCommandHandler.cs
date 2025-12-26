using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.SendEmailConfirmation
{
    public sealed class SendEmailConfirmationCommandHandler(
        IUserRepository userRepository,
        IOneTimeTokenRepository oneTimeTokenRepository,
        IOneTimeTokenService oneTimeTokenService,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        IFrontendLinkBuilder frontendLinkBuilder,
        IClock clock) : IRequestHandler<SendEmailConfirmationCommand>
    {
        public async Task Handle(
            SendEmailConfirmationCommand request,
            CancellationToken cancellationToken)
        {
            // Important: do not leak whether a user exists or not.
            var email = Email.Create(request.Email);

            User? user = await userRepository.GetByEmailAsync(
                normalizedEmail: email.Value,
                cancellationToken: cancellationToken);

            if (user is null)
                return;

            if (user.IsEmailConfirmed)
                return;

            DateTime nowUtc = clock.UtcNow;

            // Revoke previous active tokens for this purpose (optional but recommended).
            IReadOnlyList<OneTimeToken> activeTokens = await oneTimeTokenRepository.GetActive(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.EmailConfirmation,
                nowUtc: nowUtc,
                cancellationToken: cancellationToken);

            foreach (OneTimeToken t in activeTokens)
                t.Revoke(nowUtc);

            string rawToken = oneTimeTokenService.GenerateRawToken();
            string tokenHash = oneTimeTokenService.HashToken(rawToken);

            DateTime expiresAtUtc = nowUtc.Add(oneTimeTokenService.GetTtl(OneTimeTokenPurpose.EmailConfirmation));

            var token = OneTimeToken.Create(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.EmailConfirmation,
                tokenHash: tokenHash,
                expiresAtUtc: expiresAtUtc,
                createdAtUtc: nowUtc);

            await oneTimeTokenRepository.Add(
                token: token,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            string link = frontendLinkBuilder.BuildConfirmEmailLink(
                userId: user.Id,
                rawToken: rawToken);

            await emailSender.SendEmailConfirmation(
                toEmail: user.Email.Value,
                confirmationLink: link,
                cancellationToken: cancellationToken);
        }
    }
}
