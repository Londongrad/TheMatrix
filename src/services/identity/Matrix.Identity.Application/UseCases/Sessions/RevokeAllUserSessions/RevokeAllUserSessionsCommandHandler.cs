using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
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
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            user.RevokeAllRefreshTokens();

            await _userRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
