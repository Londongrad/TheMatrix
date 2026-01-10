using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ChangeAvatarFromFile
{
    public sealed record ChangeAvatarFromFileCommand(
        Stream FileStream,
        string FileName,
        string ContentType) : IRequest<string>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeAvatarChange;
    }
}
