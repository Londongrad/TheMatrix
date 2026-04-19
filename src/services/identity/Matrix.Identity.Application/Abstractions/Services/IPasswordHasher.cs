using Matrix.Identity.Domain.Entities;

namespace Matrix.Identity.Application.Abstractions.Services
{
    public interface IPasswordHasher
    {
        string Hash(string password);

        PasswordVerificationOutcome Verify(
            User user,
            string passwordHash,
            string providedPassword);
    }
}
