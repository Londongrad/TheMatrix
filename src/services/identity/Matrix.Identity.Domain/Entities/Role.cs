using Matrix.BuildingBlocks.Domain;
using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.Entities
{
    public sealed class Role
    {
        public const int NameMaxLength = 64;

        private Role() { }

        private Role(
            string name,
            bool isSystem)
        {
            Id = Guid.NewGuid();
            Name = name;
            IsSystem = isSystem;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        public string Name { get; private set; } = null!;
        public bool IsSystem { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }

        public static Role Create(
            string name,
            bool isSystem)
        {
            name = GuardHelper.AgainstNullOrWhiteSpace(
                value: name,
                errorFactory: DomainErrorsFactory.EmptyRoleName);

            if (name.Length > NameMaxLength)
                throw DomainErrorsFactory.InvalidRoleNameLength(
                    maxLength: NameMaxLength,
                    actualLength: name.Length,
                    propertyName: nameof(name));

            return new Role(
                name: name,
                isSystem: isSystem);
        }
    }
}
