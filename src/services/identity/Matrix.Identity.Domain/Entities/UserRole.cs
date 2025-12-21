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
            UserId = GuardHelper.AgainstEmptyGuid(
                id: userId,
                errorFactory: DomainErrorsFactory.EmptyUserId);
            RoleId = GuardHelper.AgainstEmptyGuid(
                id: roleId,
                errorFactory: DomainErrorsFactory.EmptyRoleId);
        }

        public Guid UserId { get; private set; }
        public Guid RoleId { get; private set; }
    }
}
