using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Security.PasswordHashing;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.PasswordHashing
{
    public sealed class PasswordHasherTests
    {
        [Fact]
        public void Hash_WhenCalledTwice_ProducesDifferentSaltedHashes()
        {
            var passwordHasher = new PasswordHasher();

            string firstHash = passwordHasher.Hash("Tr1nity!42");
            string secondHash = passwordHasher.Hash("Tr1nity!42");

            Assert.NotEqual(
                expected: firstHash,
                actual: secondHash);
        }

        [Fact]
        public void Verify_WhenPasswordMatches_ReturnsSuccess()
        {
            var passwordHasher = new PasswordHasher();
            User user = CreateUser();
            string hash = passwordHasher.Hash("N3o!42");

            PasswordVerificationOutcome result = passwordHasher.Verify(
                user: user,
                passwordHash: hash,
                providedPassword: "N3o!42");

            Assert.NotEqual(
                expected: PasswordVerificationOutcome.Failed,
                actual: result);
        }

        [Fact]
        public void Verify_WhenPasswordDoesNotMatch_ReturnsFailed()
        {
            var passwordHasher = new PasswordHasher();
            User user = CreateUser();
            string hash = passwordHasher.Hash("correct-password");

            PasswordVerificationOutcome result = passwordHasher.Verify(
                user: user,
                passwordHash: hash,
                providedPassword: "wrong-password");

            Assert.Equal(
                expected: PasswordVerificationOutcome.Failed,
                actual: result);
        }
    }
}
