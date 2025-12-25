using Matrix.Identity.Domain.Enums;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions
{
    public sealed class UserPermissionOverrideResult
    {
        public string PermissionKey { get; init; } = string.Empty;
        public PermissionEffect Effect { get; init; }
    }
}
