using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Application.Abstractions;
using Matrix.Identity.Application.Exceptions;
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
            ValidateCredentials(request);

            // создаём value objects (здесь и Trim, и ToLower, и regex-проверка)
            var email = Email.Create(request.Email);
            var username = Username.Create(request.Username);

            var emailTaken = await _userRepository
                .IsEmailTakenAsync(email.Value, cancellationToken);

            if (emailTaken)
                throw new EmailAlreadyInUseException(email.Value);

            var usernameTaken = await _userRepository
                .IsUsernameTakenAsync(username.Value, cancellationToken);

            if (usernameTaken)
                throw new UsernameAlreadyInUseException(username.Value);

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

        private static void ValidateCredentials(RegisterUserCommand request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new DomainValidationException("Email is required.", nameof(request.Email));
            }

            if (string.IsNullOrWhiteSpace(request.Username))
                throw new DomainValidationException("Username is required.", nameof(request.Username));

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new DomainValidationException("Password is required.", nameof(request.Password));
            }

            if (request.Password != request.ConfirmPassword)
            {
                throw new DomainValidationException("Password and confirmation password do not match.");
            }
        }
    }
}
