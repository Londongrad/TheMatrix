using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Sessions.RevokeAllUserSessions
{
    public sealed class RevokeAllUserSessionsCommandHandler(
        IUserRepository userRepository)
        : IRequestHandler<RevokeAllUserSessionsCommand>
    {
        private readonly IUserRepository _userRepository = userRepository;

        public async Task Handle(
            RevokeAllUserSessionsCommand request,
            CancellationToken cancellationToken)
        {
            User user = await _userRepository.GetByIdAsync(userId: request.UserId, cancellationToken: cancellationToken)
                        ?? throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            user.RevokeAllRefreshTokens();

            await _userRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
