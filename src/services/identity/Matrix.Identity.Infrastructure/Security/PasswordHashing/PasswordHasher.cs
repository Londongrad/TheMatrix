using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Matrix.Identity.Infrastructure.Security.PasswordHashing
{
    public sealed class PasswordHasher : IPasswordHasher
    {
        private static readonly object HashingContext = new();
        private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _hashingHasher = new();
        private readonly Microsoft.AspNetCore.Identity.PasswordHasher<User> _verificationHasher = new();

        public string Hash(string password)
        {
            // New-password flows can happen before we have a concrete User instance,
            // so hashing uses a dedicated non-null context instead of a fake null user.
            return _hashingHasher.HashPassword(
                user: HashingContext,
                password: password);
        }

        public PasswordVerificationOutcome Verify(
            User user,
            string passwordHash,
            string providedPassword)
        {
            PasswordVerificationResult result = _verificationHasher.VerifyHashedPassword(
                user: user ?? throw new ArgumentNullException(nameof(user)),
                hashedPassword: passwordHash,
                providedPassword: providedPassword);

            return result switch
            {
                PasswordVerificationResult.Success => PasswordVerificationOutcome.Success,
                PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationOutcome.SuccessRehashNeeded,
                _ => PasswordVerificationOutcome.Failed
            };
        }
    }
}
