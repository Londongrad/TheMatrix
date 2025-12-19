using Matrix.BuildingBlocks.Domain;
using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.Entities
{
    public sealed class UserRole
    {
        private UserRole() { }

        public UserRole(
            Guid userId,
            Guid roleId)
        {
            UserId = GuardHelper.AgainstEmptyGuid(userId, DomainErrorsFactory.EmptyUserId);
            RoleId = GuardHelper.AgainstEmptyGuid(roleId, DomainErrorsFactory.EmptyRoleId);
        }

        public Guid UserId { get; private set; }
        public Guid RoleId { get; private set; }
    }
}
