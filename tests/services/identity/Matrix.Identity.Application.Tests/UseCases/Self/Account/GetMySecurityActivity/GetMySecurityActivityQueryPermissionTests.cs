using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.GetMySecurityActivity
{
    public sealed class GetMySecurityActivityQueryPermissionTests
    {
        [Fact]
        public void Query_RequiresIdentityMeSessionsReadPermission()
        {
            var query = new GetMySecurityActivityQuery(
                Cursor: null,
                PageSize: SecurityActivityPageSizePolicy.DefaultPageSize);

            IRequirePermission permissionRequest = Assert.IsAssignableFrom<IRequirePermission>(query);

            Assert.Equal(
                expected: PermissionKeys.IdentityMeSessionsRead,
                actual: permissionRequest.PermissionKey);
        }
    }
}
