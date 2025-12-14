using Matrix.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using IPasswordHasher = Matrix.Identity.Application.Abstractions.IPasswordHasher;

namespace Matrix.Identity.Infrastructure.Security
{
    public sealed class PasswordHasherAdapter : IPasswordHasher
    {
        private readonly PasswordHasher<User> _inner = new();

        public string Hash(string password)
        {
            // временный User только для соли, но можно и настоящий, если есть
            var fakeUser = (User?)null;
            return _inner.HashPassword(
                user: fakeUser!,
                password: password);
        }

        public bool Verify(
            string passwordHash,
            string providedPassword)
        {
            var fakeUser = (User?)null;

            PasswordVerificationResult result = _inner.VerifyHashedPassword(
                user: fakeUser!,
                hashedPassword: passwordHash,
                providedPassword: providedPassword);

            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
