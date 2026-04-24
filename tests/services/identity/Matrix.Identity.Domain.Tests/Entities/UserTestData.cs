using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Domain.Tests.Entities;

internal static class UserTestData
{
    internal static readonly DateTime CreatedAtUtc = new(2047, 5, 6, 7, 8, 9, DateTimeKind.Utc);
    internal static readonly Email Email = Email.Create("neo@matrix.local");
    internal static readonly Username Username = Username.Create("neo");

    internal static User CreateUser()
    {
        return User.CreateNew(
            email: Email,
            username: Username,
            passwordHash: "hashed-password",
            createdAtUtc: CreatedAtUtc);
    }
}
