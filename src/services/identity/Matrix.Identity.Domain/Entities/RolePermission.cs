using Matrix.BuildingBlocks.Domain;
using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.Entities
{
    public sealed class RolePermission
    {
        public const int PermissionKeyMaxLength = 200;
        private RolePermission() { }

        public RolePermission(
            Guid roleId,
            string permissionKey)
        {
            RoleId = GuardHelper.AgainstEmptyGuid(
                id: roleId,
                errorFactory: DomainErrorsFactory.EmptyRoleId);

            permissionKey = GuardHelper.AgainstNullOrWhiteSpace(
                value: permissionKey,
                errorFactory: DomainErrorsFactory.EmptyPermissionKey);

            if (permissionKey.Length > PermissionKeyMaxLength)
                throw DomainErrorsFactory.InvalidPermissionKeyLength(
                    maxLength: PermissionKeyMaxLength,
                    actualLength: permissionKey.Length,
                    propertyName: nameof(permissionKey));

            PermissionKey = permissionKey;
        }

        public Guid RoleId { get; private set; }
        public string PermissionKey { get; private set; } = null!;
    }
}
