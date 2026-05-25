using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Errors;
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
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork,
        ISecurityAuditService securityAuditService) : IRequestHandler<ResetPasswordCommand>
    {
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task Handle(
            ResetPasswordCommand request,
            CancellationToken cancellationToken)
        {
            User? user = await userRepository.GetByIdWithRefreshTokensAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            if (user is null)
            {
                await WriteAuditAsync(
                    request: request,
                    isSuccessful: false,
                    userId: null,
                    details: "UserNotFound",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw DomainErrorsFactory.OneTimeTokenNotFound(nameof(request.UserId));
            }

            if (user.IsDeleted)
            {
                await WriteAuditAsync(
                    request: request,
                    isSuccessful: false,
                    userId: user.Id,
                    details: "AccountDeleted",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.AccountDeleted();
            }

            string tokenHash = oneTimeTokenService.HashToken(request.Token);

            OneTimeToken? token = await oneTimeTokenRepository.Find(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.PasswordReset,
                tokenHash: tokenHash,
                cancellationToken: cancellationToken);

            if (token is null)
            {
                await WriteAuditAsync(
                    request: request,
                    isSuccessful: false,
                    userId: user.Id,
                    details: "InvalidToken",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw DomainErrorsFactory.OneTimeTokenNotFound(nameof(request.Token));
            }

            DateTime nowUtc = _timeProvider.GetUtcNow()
               .UtcDateTime;

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
                if (!session.IsActive(nowUtc))
                    continue;

                session.Revoke(
                    reason: RefreshTokenRevocationReason.PasswordChanged,
                    revokedAtUtc: nowUtc);
            }

            await WriteAuditAsync(
                request: request,
                isSuccessful: true,
                userId: user.Id,
                details: null,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private Task WriteAuditAsync(
            ResetPasswordCommand request,
            bool isSuccessful,
            Guid? userId,
            string? details,
            CancellationToken cancellationToken)
        {
            return securityAuditService.WriteAsync(
                entry: new SecurityAuditEntry(
                    EventType: SecurityAuditEventType.PasswordResetCompleted,
                    IsSuccessful: isSuccessful,
                    UserId: userId,
                    Subject: request.UserId.ToString(),
                    IpAddress: request.IpAddress,
                    UserAgent: request.UserAgent,
                    Details: details),
                cancellationToken: cancellationToken);
        }
    }
}
