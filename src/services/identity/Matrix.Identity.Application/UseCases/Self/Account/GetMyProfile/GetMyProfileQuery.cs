using Matrix.BuildingBlocks.Application.Authorization;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile
{
    public sealed record GetMyProfileQuery : IRequest<MyProfileResult>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeProfileRead;
    }
}
