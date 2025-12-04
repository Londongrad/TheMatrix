using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.ChangeAvatar
{
    public sealed class ChangeAvatarCommandHandler(
        IUserRepository userRepository)
        : IRequestHandler<ChangeAvatarCommand>
    {
        private readonly IUserRepository _userRepository = userRepository;

        public async Task Handle(
            ChangeAvatarCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            user.ChangeAvatar(request.AvatarUrl);

            await _userRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
