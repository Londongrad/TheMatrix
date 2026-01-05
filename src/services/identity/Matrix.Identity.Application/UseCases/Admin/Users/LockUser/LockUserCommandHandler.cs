using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.LockUser
{
    public sealed class LockUserCommandHandler(
        IUserRepository userRepository,
        ISecurityStateChangeCollector securityStateChangeCollector,
        IUnitOfWork unitOfWork)
        : IRequestHandler<LockUserCommand>
    {
        public async Task Handle(
            LockUserCommand request,
            CancellationToken cancellationToken)
        {
            await unitOfWork.ExecuteInTransactionAsync(
                action: async token =>
                {
                    User user = await userRepository.GetByIdWithRefreshTokensAsync(
                                    userId: request.UserId,
                                    cancellationToken: token) ??
                                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

                    bool wasLocked = user.IsLocked;

                    user.Lock();
                    user.RevokeAllRefreshTokens(
                        reason: RefreshTokenRevocationReason.UserLocked,
                        revokedAtUtc: DateTime.UtcNow);

                    if (!wasLocked)
                        securityStateChangeCollector.MarkUserChanged(user.Id);
                },
                cancellationToken: cancellationToken);
        }
    }
}
