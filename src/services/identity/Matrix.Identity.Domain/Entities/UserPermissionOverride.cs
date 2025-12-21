using Matrix.BuildingBlocks.Domain;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.Entities
{
    public sealed class UserPermissionOverride
    {
        public const int PermissionKeyMaxLength = 200;

        private UserPermissionOverride() { }

        public UserPermissionOverride(
            Guid userId,
            string permissionKey,
            PermissionEffect effect)
        {
            UserId = GuardHelper.AgainstEmptyGuid(
                id: userId,
                errorFactory: DomainErrorsFactory.EmptyUserId);

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

        public Guid UserId { get; private set; }
        public string PermissionKey { get; private set; } = null!;
        public PermissionEffect Effect { get; private set; }

        public void SetEffect(PermissionEffect effect)
        {
            Effect = effect;
        }
    }
}
