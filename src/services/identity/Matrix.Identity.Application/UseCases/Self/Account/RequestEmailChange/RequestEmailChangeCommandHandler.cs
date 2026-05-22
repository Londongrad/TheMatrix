using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange
{
    public sealed class RequestEmailChangeCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IPendingEmailChangeDeliveryService pendingEmailChangeDeliveryService,
        ISecurityAuditService securityAuditService,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser)
        : IRequestHandler<RequestEmailChangeCommand, string>
    {
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task<string> Handle(
            RequestEmailChangeCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            var newEmail = Email.Create(request.NewEmail);

            if (string.Equals(
                    a: user.Email.Value,
                    b: newEmail.Value,
                    comparisonType: StringComparison.Ordinal))
            {
                await WriteAuditAsync(
                    user: user,
                    subject: newEmail.Value,
                    isSuccessful: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    details: "SameAsCurrentEmail",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.EmailChangeRequiresDifferentAddress();
            }

            PasswordVerificationOutcome currentPasswordVerification = passwordHasher.Verify(
                user: user,
                passwordHash: user.PasswordHash,
                providedPassword: request.CurrentPassword);

            if (!currentPasswordVerification.Succeeded)
            {
                await WriteAuditAsync(
                    user: user,
                    subject: newEmail.Value,
                    isSuccessful: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    details: "InvalidCurrentPassword",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.InvalidCurrentPassword();
            }

            User? existingEmailOwner = await userRepository.GetByEmailAsync(
                normalizedEmail: newEmail.Value,
                cancellationToken: cancellationToken);

            if (existingEmailOwner is not null && existingEmailOwner.Id != user.Id)
            {
                await WriteAuditAsync(
                    user: user,
                    subject: newEmail.Value,
                    isSuccessful: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    details: "EmailAlreadyInUse",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.EmailAlreadyInUse(newEmail.Value);
            }

            User? existingPendingOwner = await userRepository.GetByPendingEmailAsync(
                normalizedEmail: newEmail.Value,
                cancellationToken: cancellationToken);

            if (existingPendingOwner is not null && existingPendingOwner.Id != user.Id)
            {
                await WriteAuditAsync(
                    user: user,
                    subject: newEmail.Value,
                    isSuccessful: false,
                    ipAddress: request.IpAddress,
                    userAgent: request.UserAgent,
                    details: "PendingEmailAlreadyInUse",
                    cancellationToken: cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw ApplicationErrorsFactory.PendingEmailAlreadyInUse(newEmail.Value);
            }

            DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

            user.RequestEmailChange(
                newEmail: newEmail,
                requestedAtUtc: nowUtc);

            await pendingEmailChangeDeliveryService.SendConfirmationAsync(
                user: user,
                pendingEmail: user.PendingEmail!,
                ipAddress: request.IpAddress,
                userAgent: request.UserAgent,
                eventType: SecurityAuditEventType.EmailChangeRequested,
                cancellationToken: cancellationToken);

            return user.PendingEmail!;
        }

        private Task WriteAuditAsync(
            User user,
            string subject,
            bool isSuccessful,
            string? ipAddress,
            string? userAgent,
            string? details,
            CancellationToken cancellationToken)
        {
            return securityAuditService.WriteAsync(
                entry: new SecurityAuditEntry(
                    EventType: SecurityAuditEventType.EmailChangeRequested,
                    IsSuccessful: isSuccessful,
                    UserId: user.Id,
                    SessionId: null,
                    Subject: subject,
                    IpAddress: ipAddress,
                    UserAgent: userAgent,
                    DeviceId: null,
                    DeviceName: null,
                    Details: details),
                cancellationToken: cancellationToken);
        }
    }
}
