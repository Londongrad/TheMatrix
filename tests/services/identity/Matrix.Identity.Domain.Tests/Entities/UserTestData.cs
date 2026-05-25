using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Domain.Tests.Entities
{
    internal static class UserTestData
    {
        internal static readonly DateTime CreatedAtUtc = new(
            year: 2047,
            month: 5,
            day: 6,
            hour: 7,
            minute: 8,
            second: 9,
            kind: DateTimeKind.Utc);

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
}
