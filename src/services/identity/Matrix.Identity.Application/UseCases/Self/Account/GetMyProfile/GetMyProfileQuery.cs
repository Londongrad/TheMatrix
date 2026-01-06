using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile
{
    public sealed record GetMyProfileQuery : IRequest<MyProfileResult>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeProfileRead;
    }
}
