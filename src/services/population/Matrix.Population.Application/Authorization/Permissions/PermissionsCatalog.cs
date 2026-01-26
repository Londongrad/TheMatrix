using Matrix.PermissionCatalog.Abstractions;
using ContractPermissionsCatalog = Matrix.Population.Contracts.Authorization.Permissions.PermissionsCatalog;

namespace Matrix.Population.Application.Authorization.Permissions
{
    public static class PermissionsCatalog
    {
        public static readonly IReadOnlyList<PermissionDefinition> All = ContractPermissionsCatalog.All;
    }
}
