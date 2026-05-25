using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmailChange
{
    public sealed class ConfirmEmailChangeCommandHandler(
        IUserRepository userRepository,
        IOneTimeTokenRepository oneTimeTokenRepository,
        IOneTimeTokenService oneTimeTokenService,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork,
        ISecurityAuditService securityAuditService)
        : IRequestHandler<ConfirmEmailChangeCommand>
    {
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task Handle(
            ConfirmEmailChangeCommand request,
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

            if (string.IsNullOrWhiteSpace(user.PendingEmail))
            {
                await WriteAuditAsync(
                    request: request,
                    userId: user.Id,
                    subject: user.Email.Value,
                    isSuccessful: false,
                    details: "PendingEmailMissing",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.EmailChangePendingEmailMissing();
            }

            string tokenHash = oneTimeTokenService.HashToken(request.Token);

            OneTimeToken? token = await oneTimeTokenRepository.Find(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.EmailChange,
                tokenHash: tokenHash,
                cancellationToken: cancellationToken);

            if (token is null)
            {
                await WriteAuditAsync(
                    request: request,
                    userId: user.Id,
                    subject: user.PendingEmail,
                    isSuccessful: false,
                    details: "InvalidToken",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw DomainErrorsFactory.OneTimeTokenNotFound(nameof(request.Token));
            }

            User? existingEmailOwner = await userRepository.GetByEmailAsync(
                normalizedEmail: user.PendingEmail,
                cancellationToken: cancellationToken);

            if (existingEmailOwner is not null && existingEmailOwner.Id != user.Id)
            {
                await WriteAuditAsync(
                    request: request,
                    userId: user.Id,
                    subject: user.PendingEmail,
                    isSuccessful: false,
                    details: "EmailAlreadyInUse",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.EmailAlreadyInUse(user.PendingEmail);
            }

            DateTime nowUtc = _timeProvider.GetUtcNow()
               .UtcDateTime;
            string previousEmail = user.Email.Value;
            string newEmail = user.PendingEmail;

            token.MarkUsed(nowUtc);
            user.ConfirmPendingEmailChange(nowUtc);

            await WriteAuditAsync(
                request: request,
                userId: user.Id,
                subject: newEmail,
                isSuccessful: true,
                details: $"PreviousEmail:{previousEmail}",
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private Task WriteAuditAsync(
            ConfirmEmailChangeCommand request,
            Guid? userId,
            string subject,
            bool isSuccessful,
            string? details,
            CancellationToken cancellationToken)
        {
            return securityAuditService.WriteAsync(
                entry: new SecurityAuditEntry(
                    EventType: SecurityAuditEventType.EmailChanged,
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
