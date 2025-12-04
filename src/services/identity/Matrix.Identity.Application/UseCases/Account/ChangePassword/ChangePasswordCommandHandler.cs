using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.ChangePassword
{
    public sealed class ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
        : IRequestHandler<ChangePasswordCommand>
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;

        public async Task Handle(
            ChangePasswordCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken) 
                ?? throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            // проверяем текущий пароль
            var isCurrentValid = _passwordHasher.Verify(user.PasswordHash, request.CurrentPassword);
            if (!isCurrentValid)
            {
                throw ApplicationErrorsFactory.InvalidCurrentPassword();
            }

            // хэшируем новый пароль и сохраняем в домен
            var newPasswordHash = _passwordHasher.Hash(request.NewPassword);
            user.ChangePasswordHash(newPasswordHash);

            await _userRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
