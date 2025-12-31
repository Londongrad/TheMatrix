using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.LockUser
{
    public sealed class LockUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<LockUserCommand>
    {
        public async Task Handle(
            LockUserCommand request,
            CancellationToken cancellationToken)
        {
            User user = await userRepository.GetByIdAsync(
                            userId: request.UserId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            user.Lock();
            user.RevokeAllRefreshTokens();

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
