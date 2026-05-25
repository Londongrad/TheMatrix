using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities
{
    public sealed class RoleTests
    {
        private static readonly DateTime CreatedAtUtc = new(
            year: 2047,
            month: 4,
            day: 5,
            hour: 6,
            minute: 7,
            second: 8,
            kind: DateTimeKind.Utc);

        [Fact]
        public void Create_WithValidValues_SetsProperties()
        {
            var role = Role.Create(
                name: " Manager ",
                isSystem: false,
                createdAtUtc: CreatedAtUtc);

            Assert.NotEqual(
                expected: Guid.Empty,
                actual: role.Id);
            Assert.Equal(
                expected: "Manager",
                actual: role.Name);
            Assert.Equal(
                expected: "MANAGER",
                actual: role.NormalizedName);
            Assert.False(role.IsSystem);
            Assert.Equal(
                expected: CreatedAtUtc,
                actual: role.CreatedAtUtc);
        }

        [Fact]
        public void Create_WithWhitespaceName_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => Role.Create(
                name: "   ",
                isSystem: true,
                createdAtUtc: CreatedAtUtc));

            Assert.Equal(
                expected: "Identity.Role.Name.Empty",
                actual: exception.Code);
            Assert.Equal(
                expected: "name",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithTooLongName_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => Role.Create(
                name: new string(
                    c: 'R',
                    count: Role.NameMaxLength + 1),
                isSystem: false,
                createdAtUtc: CreatedAtUtc));

            Assert.Equal(
                expected: "Identity.Role.Name.InvalidLength",
                actual: exception.Code);
            Assert.Equal(
                expected: "name",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Rename_WithValidName_UpdatesNameAndNormalizedName()
        {
            var role = Role.Create(
                name: "Manager",
                isSystem: false,
                createdAtUtc: CreatedAtUtc);

            role.Rename(" auditor ");

            Assert.Equal(
                expected: "auditor",
                actual: role.Name);
            Assert.Equal(
                expected: "AUDITOR",
                actual: role.NormalizedName);
        }

        [Fact]
        public void MarkAsSystem_SetsFlagToTrue()
        {
            var role = Role.Create(
                name: "Manager",
                isSystem: false,
                createdAtUtc: CreatedAtUtc);

            role.MarkAsSystem();

            Assert.True(role.IsSystem);
        }
    }
}
