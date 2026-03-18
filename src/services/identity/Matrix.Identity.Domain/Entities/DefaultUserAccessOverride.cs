using Matrix.BuildingBlocks.Domain;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.Entities
{
    public sealed class DefaultUserAccessOverride
    {
        public const int PermissionKeyMaxLength = 200;

        private DefaultUserAccessOverride() { }

        public DefaultUserAccessOverride(
            Guid policyId,
            string permissionKey,
            PermissionEffect effect)
        {
            PolicyId = GuardHelper.AgainstEmptyGuid(
                id: policyId,
                errorFactory: DomainErrorsFactory.EmptyId);

            permissionKey = GuardHelper.AgainstNullOrWhiteSpace(
                value: permissionKey,
                errorFactory: DomainErrorsFactory.EmptyPermissionKey);

            if (permissionKey.Length > PermissionKeyMaxLength)
                throw DomainErrorsFactory.InvalidPermissionKeyLength(
                    maxLength: PermissionKeyMaxLength,
                    actualLength: permissionKey.Length,
                    propertyName: nameof(permissionKey));

            PermissionKey = permissionKey;
            Effect = effect;
        }

        public Guid PolicyId { get; private set; }
        public string PermissionKey { get; private set; } = null!;
        public PermissionEffect Effect { get; private set; }

        public void SetEffect(PermissionEffect effect)
        {
            Effect = effect;
        }
    }
}
