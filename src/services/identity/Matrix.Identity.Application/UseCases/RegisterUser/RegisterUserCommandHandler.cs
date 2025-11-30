using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using MediatR;

namespace Matrix.Identity.Application.UseCases.RegisterUser
{
    public sealed class RegisterUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
        : IRequestHandler<RegisterUserCommand, RegisterUserResult>
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;

        public async Task<RegisterUserResult> Handle(
            RegisterUserCommand request,
            CancellationToken cancellationToken)
        {
            // создаём value objects (здесь и Trim, и ToLower, и regex-проверка)
            var email = Email.Create(request.Email);
            var username = Username.Create(request.Username);

            var emailTaken = await _userRepository
                .IsEmailTakenAsync(email.Value, cancellationToken);

            if (emailTaken)
                throw ApplicationErrorsFactory.EmailAlreadyInUse(email.Value);

            var usernameTaken = await _userRepository
                .IsUsernameTakenAsync(username.Value, cancellationToken);

            if (usernameTaken)
                throw ApplicationErrorsFactory.UsernameAlreadyInUse(username.Value);

            var passwordHash = _passwordHasher.Hash(request.Password);

            var user = User.CreateNew(email, username, passwordHash);

            await _userRepository.AddAsync(user, cancellationToken);
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
