using Matrix.Identity.Infrastructure.Security.PasswordHashing;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.PasswordHashing;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_WhenCalledTwice_ProducesDifferentSaltedHashes()
    {
        var passwordHasher = new PasswordHasher();

        string firstHash = passwordHasher.Hash("Tr1nity!42");
        string secondHash = passwordHasher.Hash("Tr1nity!42");

        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void Verify_WhenPasswordMatches_ReturnsSuccess()
    {
        var passwordHasher = new PasswordHasher();
        var user = CreateUser();
        string hash = passwordHasher.Hash("N3o!42");

        var result = passwordHasher.Verify(user, hash, "N3o!42");

        Assert.NotEqual(Matrix.Identity.Application.Abstractions.Services.PasswordVerificationOutcome.Failed, result);
    }

    [Fact]
    public void Verify_WhenPasswordDoesNotMatch_ReturnsFailed()
    {
        var passwordHasher = new PasswordHasher();
        var user = CreateUser();
        string hash = passwordHasher.Hash("correct-password");

        var result = passwordHasher.Verify(user, hash, "wrong-password");

        Assert.Equal(Matrix.Identity.Application.Abstractions.Services.PasswordVerificationOutcome.Failed, result);
    }
}
