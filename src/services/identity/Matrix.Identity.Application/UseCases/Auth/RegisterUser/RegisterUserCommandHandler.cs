using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Auth.RegisterUser
{
    public sealed class RegisterUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
        : IRequestHandler<RegisterUserCommand, RegisterUserResult>
    {
        public async Task<RegisterUserResult> Handle(
            RegisterUserCommand request,
            CancellationToken cancellationToken)
        {
            // создаём value objects (здесь и Trim, и ToLower, и regex-проверка)
            var email = Email.Create(request.Email);
            var username = Username.Create(request.Username);

            bool emailTaken = await userRepository
               .IsEmailTakenAsync(
                    normalizedEmail: email.Value,
                    cancellationToken: cancellationToken);

            if (emailTaken)
                throw ApplicationErrorsFactory.EmailAlreadyInUse(email.Value);

            bool usernameTaken = await userRepository
               .IsUsernameTakenAsync(
                    normalizedUsername: username.Value,
                    cancellationToken: cancellationToken);

            if (usernameTaken)
                throw ApplicationErrorsFactory.UsernameAlreadyInUse(username.Value);

            string passwordHash = passwordHasher.Hash(request.Password);

            var user = User.CreateNew(
                email: email,
                username: username,
                passwordHash: passwordHash);

            await userRepository.AddAsync(
                user: user,
                cancellationToken: cancellationToken);
            await userRepository.SaveChangesAsync(cancellationToken);

            return new RegisterUserResult
            {
                UserId = user.Id,
                Username = user.Username.Value,
                Email = user.Email.Value
            };
        }
    }
}
