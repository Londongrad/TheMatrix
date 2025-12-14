using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
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
        public async Task Handle(
            ChangePasswordCommand request,
            CancellationToken cancellationToken)
        {
            User user = await userRepository.GetByIdAsync(
                            userId: request.UserId,
                            cancellationToken: cancellationToken) ??
                        throw ApplicationErrorsFactory.UserNotFound(request.UserId);

            // проверяем текущий пароль
            bool isCurrentValid = passwordHasher.Verify(
                passwordHash: user.PasswordHash,
                providedPassword: request.CurrentPassword);
            if (!isCurrentValid)
                throw ApplicationErrorsFactory.InvalidCurrentPassword();

            // хэшируем новый пароль и сохраняем в домен
            string newPasswordHash = passwordHasher.Hash(request.NewPassword);
            user.ChangePasswordHash(newPasswordHash);

            await userRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
