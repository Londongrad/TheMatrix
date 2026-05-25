using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities
{
    public sealed class PermissionTests
    {
        [Fact]
        public void Constructor_WithValidValues_SetsProperties()
        {
            var permission = new Permission(
                key: "identity.users.read",
                service: "identity",
                group: "users",
                description: "Read users.");

            Assert.Equal(
                expected: "identity.users.read",
                actual: permission.Key);
            Assert.Equal(
                expected: "identity",
                actual: permission.Service);
            Assert.Equal(
                expected: "users",
                actual: permission.Group);
            Assert.Equal(
                expected: "Read users.",
                actual: permission.Description);
            Assert.False(permission.IsDeprecated);
        }

        [Fact]
        public void UpdateMetadata_UpdatesProperties()
        {
            var permission = new Permission(
                key: "identity.users.read",
                service: "identity",
                group: "users",
                description: "Read users.");

            permission.UpdateMetadata(
                service: "iam",
                group: "accounts",
                description: "Read account data.");

            Assert.Equal(
                expected: "iam",
                actual: permission.Service);
            Assert.Equal(
                expected: "accounts",
                actual: permission.Group);
            Assert.Equal(
                expected: "Read account data.",
                actual: permission.Description);
        }

        [Fact]
        public void Deprecate_AndActivate_ToggleDeprecatedFlag()
        {
            var permission = new Permission(
                key: "identity.users.read",
                service: "identity",
                group: "users",
                description: "Read users.");

            permission.Deprecate();
            Assert.True(permission.IsDeprecated);

            permission.Activate();
            Assert.False(permission.IsDeprecated);
        }
    }
}
