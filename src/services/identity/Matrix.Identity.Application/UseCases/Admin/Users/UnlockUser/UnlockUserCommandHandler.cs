using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.UnlockUser
{
    public sealed class UnlockUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<UnlockUserCommand>
    {
        public async Task Handle(
            UnlockUserCommand request,
            CancellationToken cancellationToken)
        {
            User user = await userRepository.GetByIdAsync(
                            userId: request.UserId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            user.Unlock();

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
