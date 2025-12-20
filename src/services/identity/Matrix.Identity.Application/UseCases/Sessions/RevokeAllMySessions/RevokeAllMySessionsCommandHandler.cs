using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Sessions.RevokeAllMySessions
{
    public sealed class RevokeAllMySessionsCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser)
        : IRequestHandler<RevokeAllMySessionsCommand>
    {
        public async Task Handle(
            RevokeAllMySessionsCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            user.RevokeAllRefreshTokens();

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
