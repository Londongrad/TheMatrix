using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmail
{
    public sealed class ConfirmEmailCommandHandler(
        IUserRepository userRepository,
        IOneTimeTokenRepository oneTimeTokenRepository,
        IOneTimeTokenService oneTimeTokenService,
        IClock clock,
        IUnitOfWork unitOfWork,
        ISecurityAuditService securityAuditService)
        : IRequestHandler<ConfirmEmailCommand>
    {
        public async Task Handle(
            ConfirmEmailCommand request,
            CancellationToken cancellationToken)
        {
            User? user = await userRepository.GetByIdAsync(
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

            if (user.IsEmailConfirmed)
            {
                await WriteAuditAsync(
                    request: request,
                    isSuccessful: true,
                    userId: user.Id,
                    details: "AlreadyConfirmed",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            string tokenHash = oneTimeTokenService.HashToken(request.Token);

            OneTimeToken? token = await oneTimeTokenRepository.Find(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.EmailConfirmation,
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

            DateTime nowUtc = clock.UtcNow;

            token.MarkUsed(nowUtc);
            user.ConfirmEmail();

            await WriteAuditAsync(
                request: request,
                isSuccessful: true,
                userId: user.Id,
                details: null,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private Task WriteAuditAsync(
            ConfirmEmailCommand request,
            bool isSuccessful,
            Guid? userId,
            string? details,
            CancellationToken cancellationToken)
        {
            return securityAuditService.WriteAsync(
                entry: new SecurityAuditEntry(
                    EventType: SecurityAuditEventType.EmailConfirmed,
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
