using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Auth.ConfirmAccountRecovery
{
    public sealed class ConfirmAccountRecoveryCommandHandler(
        IUserRepository userRepository,
        IOneTimeTokenRepository oneTimeTokenRepository,
        IOneTimeTokenService oneTimeTokenService,
        IClock clock,
        IUnitOfWork unitOfWork,
        ISecurityAuditService securityAuditService)
        : IRequestHandler<ConfirmAccountRecoveryCommand>
    {
        public async Task Handle(
            ConfirmAccountRecoveryCommand request,
            CancellationToken cancellationToken)
        {
            User? user = await userRepository.GetByIdAsync(
                userId: request.UserId,
                cancellationToken: cancellationToken);

            if (user is null)
            {
                await WriteAuditAsync(
                    request: request,
                    userId: null,
                    subject: request.UserId.ToString(),
                    isSuccessful: false,
                    details: "UserNotFound",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw DomainErrorsFactory.OneTimeTokenNotFound(nameof(request.UserId));
            }

            string tokenHash = oneTimeTokenService.HashToken(request.Token);

            OneTimeToken? token = await oneTimeTokenRepository.Find(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.AccountRecovery,
                tokenHash: tokenHash,
                cancellationToken: cancellationToken);

            if (token is null)
            {
                await WriteAuditAsync(
                    request: request,
                    userId: user.Id,
                    subject: user.Email.Value,
                    isSuccessful: false,
                    details: "InvalidToken",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw DomainErrorsFactory.OneTimeTokenNotFound(nameof(request.Token));
            }

            DateTime nowUtc = clock.UtcNow;
            token.MarkUsed(nowUtc);

            string details = "AlreadyActive";

            if (user.IsDeleted)
            {
                user.Restore();
                user.BumpPermissionsVersion();
                details = user.IsLocked
                    ? "RestoredButLocked"
                    : "Restored";
            }

            await WriteAuditAsync(
                request: request,
                userId: user.Id,
                subject: user.Email.Value,
                isSuccessful: true,
                details: details,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private Task WriteAuditAsync(
            ConfirmAccountRecoveryCommand request,
            Guid? userId,
            string subject,
            bool isSuccessful,
            string? details,
            CancellationToken cancellationToken)
        {
            return securityAuditService.WriteAsync(
                entry: new SecurityAuditEntry(
                    EventType: SecurityAuditEventType.AccountRestored,
                    IsSuccessful: isSuccessful,
                    UserId: userId,
                    SessionId: null,
                    Subject: subject,
                    IpAddress: request.IpAddress,
                    UserAgent: request.UserAgent,
                    DeviceId: null,
                    DeviceName: null,
                    Details: details),
                cancellationToken: cancellationToken);
        }
    }
}
