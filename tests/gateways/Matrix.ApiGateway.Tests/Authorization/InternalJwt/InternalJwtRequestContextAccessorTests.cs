using Matrix.ApiGateway.Authorization.InternalJwt;
using Xunit;

namespace Matrix.ApiGateway.Tests.Authorization.InternalJwt
{
    public sealed class InternalJwtRequestContextAccessorTests
    {
        [Fact]
        public void Push_WhenNestedScopesAreDisposed_RestoresPreviousContext()
        {
            var accessor = new InternalJwtRequestContextAccessor();
            InternalJwtRequestContext outer = new(
                UserId: Guid.Parse("9978e6e1-9cef-4d73-a0e6-4371fec2c351"),
                Jti: "outer-jti",
                PermissionsVersion: 3,
                EffectivePermissions: ["city.read"]);
            InternalJwtRequestContext inner = new(
                UserId: Guid.Parse("50e9d10d-a3ca-48af-a2c5-35c04ddf7b1f"),
                Jti: "inner-jti",
                PermissionsVersion: 7,
                EffectivePermissions: ["city.write"]);

            using IDisposable outerScope = accessor.Push(outer);
            using IDisposable innerScope = accessor.Push(inner);

            Assert.Equal(
                expected: inner,
                actual: accessor.Current);

            innerScope.Dispose();

            Assert.Equal(
                expected: outer,
                actual: accessor.Current);

            outerScope.Dispose();

            Assert.Null(accessor.Current);
        }

        [Fact]
        public void Dispose_WhenCalledTwice_IsIdempotent()
        {
            var accessor = new InternalJwtRequestContextAccessor();
            using IDisposable scope = accessor.Push(
                new InternalJwtRequestContext(
                    UserId: Guid.Parse("db58d246-c536-4b55-967c-78fdaaa67b84"),
                    Jti: null,
                    PermissionsVersion: 11,
                    EffectivePermissions: ["city.admin"]));

            scope.Dispose();
            scope.Dispose();

            Assert.Null(accessor.Current);
        }
    }
}
