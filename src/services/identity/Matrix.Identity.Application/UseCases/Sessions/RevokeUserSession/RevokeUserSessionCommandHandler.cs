using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Sessions.RevokeUserSession
{
    public sealed class RevokeUserSessionCommandHandler(
        IUserRepository userRepository)
        : IRequestHandler<RevokeUserSessionCommand>
    {
        private readonly IUserRepository _userRepository = userRepository;

        public async Task Handle(
            RevokeUserSessionCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            // Домен сам решает, есть такой токен или нет.
            // Если нет – просто ничего не сделает (idempotent).
            user.RevokeRefreshToken(request.SessionId);

            await _userRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
