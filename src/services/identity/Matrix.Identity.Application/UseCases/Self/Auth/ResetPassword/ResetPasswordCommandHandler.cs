using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.ResetPassword
{
    public sealed class ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IUserSessionRepository userSessionRepository,
        IOneTimeTokenRepository oneTimeTokenRepository,
        IOneTimeTokenService oneTimeTokenService,
        IPasswordHasher passwordHasher,
        IClock clock,
        IUnitOfWork unitOfWork) : IRequestHandler<ResetPasswordCommand>
    {
        public async Task Handle(
            ResetPasswordCommand request,
            CancellationToken cancellationToken)
        {
            User? user = await userRepository.GetByIdWithRefreshTokensAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            if (user is null)
                throw DomainErrorsFactory.OneTimeTokenNotFound(nameof(request.UserId));

            string tokenHash = oneTimeTokenService.HashToken(request.Token);

            OneTimeToken? token = await oneTimeTokenRepository.Find(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.PasswordReset,
                tokenHash: tokenHash,
                cancellationToken: cancellationToken);

            if (token is null)
                throw DomainErrorsFactory.OneTimeTokenNotFound(nameof(request.Token));

            DateTime nowUtc = clock.UtcNow;

            token.MarkUsed(nowUtc);

            string newPasswordHash = passwordHasher.Hash(request.NewPassword);
            user.ChangePasswordHash(newPasswordHash);
            user.RevokeAllRefreshTokens(
                reason: RefreshTokenRevocationReason.PasswordChanged,
                revokedAtUtc: nowUtc);

            IReadOnlyCollection<UserSession> sessions = await userSessionRepository.ListByUserIdAsync(
                userId: user.Id,
                cancellationToken: cancellationToken);

            foreach (UserSession session in sessions)
            {
                if (!session.IsActive())
                    continue;

                session.Revoke(
                    reason: RefreshTokenRevocationReason.PasswordChanged,
                    revokedAtUtc: nowUtc);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
