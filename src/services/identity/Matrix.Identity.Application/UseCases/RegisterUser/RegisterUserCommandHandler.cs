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
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required.", nameof(request.Email));
            }

            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ArgumentException("Username is required.", nameof(request.Username));

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Password is required.", nameof(request.Password));
            }

            if (request.Password != request.ConfirmPassword)
            {
                throw new ArgumentException("Password and confirmation password do not match.");
            }

            // создаём value object Email (здесь и Trim, и ToLower, и regex-проверка)
            var email = Email.Create(request.Email);

            var emailTaken = await _userRepository
                .IsEmailTakenAsync(email.Value, cancellationToken);

            var normalizedUsername = request.Username.Trim().ToLowerInvariant();

            if (await _userRepository.IsUsernameTakenAsync(normalizedUsername, cancellationToken))
                throw new UsernameAlreadyInUseException(normalizedUsername);

            if (emailTaken)
            {
                throw new EmailAlreadyInUseException(email.Value);
            }

            var passwordHash = _passwordHasher.Hash(request.Password);

            var user = User.CreateNew(email, normalizedUsername, passwordHash);

            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            return new RegisterUserResult
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email.Value
            };
        }
    }
}
