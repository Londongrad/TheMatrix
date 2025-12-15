using Matrix.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using IPasswordHasher = Matrix.Identity.Application.Abstractions.Services.IPasswordHasher;

namespace Matrix.Identity.Infrastructure.Security.PasswordHashing
{
    public sealed class PasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher<User> _inner = new();

        public string Hash(string password)
        {
            // временный User только для соли, но можно и настоящий, если есть
            User? fakeUser = null;
            return _inner.HashPassword(
                user: fakeUser!,
                password: password);
        }

        public bool Verify(
            string passwordHash,
            string providedPassword)
        {
            User? fakeUser = null;

            PasswordVerificationResult result = _inner.VerifyHashedPassword(
                user: fakeUser!,
                hashedPassword: passwordHash,
                providedPassword: providedPassword);

            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
