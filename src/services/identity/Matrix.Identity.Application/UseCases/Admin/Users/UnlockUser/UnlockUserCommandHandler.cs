using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.Administration;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.UnlockUser
{
    public sealed class UnlockUserCommandHandler(
        IUserRepository userRepository,
        IAdminUserGuard adminUserGuard,
        ISecurityStateChangeCollector securityStateChangeCollector,
        IUnitOfWork unitOfWork)
        : IRequestHandler<UnlockUserCommand>
    {
        public async Task Handle(
            UnlockUserCommand request,
            CancellationToken cancellationToken)
        {
            await unitOfWork.ExecuteInTransactionAsync(
                action: async token =>
                {
                    User user = await userRepository.GetByIdAsync(
                                    userId: request.UserId,
                                    cancellationToken: token) ??
                                throw ApplicationErrorsFactory.UserNotFound(request.UserId);

                    await adminUserGuard.EnsureUserCanBeManagedAsync(
                        targetUserId: user.Id,
                        cancellationToken: token);

                    bool wasLocked = user.IsLocked;

                    user.Unlock();

                    if (wasLocked)
                        securityStateChangeCollector.MarkUserChanged(user.Id);
                },
                cancellationToken: cancellationToken);
        }
    }
}