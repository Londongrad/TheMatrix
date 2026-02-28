using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ClearAvatar
{
    public sealed record ClearAvatarCommand : IRequest<string?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeAvatarChange;
    }
}
