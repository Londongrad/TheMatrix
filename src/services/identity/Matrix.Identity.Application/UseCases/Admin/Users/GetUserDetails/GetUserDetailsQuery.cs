using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUserDetails
{
    public sealed record GetUserDetailsQuery(Guid UserId)
        : IRequest<UserDetailsResult>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUsersRead;
    }
}
