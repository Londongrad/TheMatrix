using Matrix.PermissionCatalog.Abstractions;
using ContractPermissionsCatalog = Matrix.Identity.Contracts.Authorization.Permissions.PermissionsCatalog;

namespace Matrix.Identity.Application.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        public static readonly IReadOnlyList<PermissionDefinition> All = ContractPermissionsCatalog.All;
    }
}