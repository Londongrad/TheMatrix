using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Account.ChangePassword
{
    public sealed class ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
        : IRequestHandler<ChangePasswordCommand>
    {
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly IUserRepository _userRepository = userRepository;

        public async Task Handle(
            ChangePasswordCommand request,
            CancellationToken cancellationToken)
        {
            User user = await _userRepository.GetByIdAsync(userId: request.UserId, cancellationToken: cancellationToken)
                        ?? throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            // проверяем текущий пароль
            bool isCurrentValid = _passwordHasher.Verify(passwordHash: user.PasswordHash,
                providedPassword: request.CurrentPassword);
            if (!isCurrentValid) throw ApplicationErrorsFactory.InvalidCurrentPassword();

            // хэшируем новый пароль и сохраняем в домен
            string newPasswordHash = _passwordHasher.Hash(request.NewPassword);
            user.ChangePasswordHash(newPasswordHash);

            await _userRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
