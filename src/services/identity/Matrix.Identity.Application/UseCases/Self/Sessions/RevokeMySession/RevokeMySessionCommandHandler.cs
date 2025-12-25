using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Extensions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Sessions.RevokeMySession
{
    public sealed class RevokeMySessionCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser)
        : IRequestHandler<RevokeMySessionCommand>
    {
        public async Task Handle(
            RevokeMySessionCommand request,
            CancellationToken cancellationToken)
        {
            Guid userId = currentUser.GetUserIdOrThrow();

            User user = await userRepository.GetByIdWithRefreshTokensAsync(
                            userId: userId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(userId);

            // Домен сам решает, есть такой токен или нет.
            // Если нет – просто ничего не сделает (idempotent).
            user.RevokeRefreshToken(request.SessionId, RefreshTokenRevocationReason.UserRevoked);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
