using Matrix.Identity.Application.Abstractions;
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
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<RegisterUserResult> Handle(
            RegisterUserCommand request,
            CancellationToken cancellationToken)
        {
            // создаём value objects (здесь и Trim, и ToLower, и regex-проверка)
            var email = Email.Create(request.Email);
            var username = Username.Create(request.Username);

            bool emailTaken = await _userRepository
                .IsEmailTakenAsync(normalizedEmail: email.Value, cancellationToken: cancellationToken);

            if (emailTaken)
                throw ApplicationErrorsFactory.EmailAlreadyInUse(email.Value);

            bool usernameTaken = await _userRepository
                .IsUsernameTakenAsync(normalizedUsername: username.Value, cancellationToken: cancellationToken);

            if (usernameTaken)
                throw ApplicationErrorsFactory.UsernameAlreadyInUse(username.Value);

            string passwordHash = _passwordHasher.Hash(request.Password);

            var user = User.CreateNew(email: email, username: username, passwordHash: passwordHash);

            await _userRepository.AddAsync(user: user, cancellationToken: cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            return new RegisterUserResult
            {
                UserId = user.Id,
                Username = user.Username.Value,
                Email = user.Email.Value
            };
        }
    }
}
