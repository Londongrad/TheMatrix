using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Sessions.RevokeUserSession
{
    public sealed class RevokeUserSessionCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<RevokeUserSessionCommand>
    {
        public async Task Handle(
            RevokeUserSessionCommand request,
            CancellationToken cancellationToken)
        {
            User user = await userRepository.GetByIdAsync(
                            userId: request.UserId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            // Домен сам решает, есть такой токен или нет.
            // Если нет – просто ничего не сделает (idempotent).
            user.RevokeRefreshToken(request.SessionId);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
