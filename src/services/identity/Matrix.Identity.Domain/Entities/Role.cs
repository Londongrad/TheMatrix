using Matrix.BuildingBlocks.Domain;
using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.Entities
{
    public sealed class Role
    {
        public const int NameMaxLength = 20;

        private Role() { }

        private Role(
            string name,
            string normalizedName,
            bool isSystem)
        {
            Id = Guid.NewGuid();
            Name = name;
            NormalizedName = normalizedName;
            IsSystem = isSystem;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        public string Name { get; private set; } = null!;
        public string NormalizedName { get; private set; } = null!;
        public bool IsSystem { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }

        public static Role Create(
            string name,
            bool isSystem)
        {
            name = GuardHelper.AgainstNullOrWhiteSpace(
                value: name,
                errorFactory: DomainErrorsFactory.EmptyRoleName,
                trim: true);

            string normalizedName = NormalizeName(name);

            ValidateName(name);

            return new Role(
                name: name,
                normalizedName: normalizedName,
                isSystem: isSystem);
        }

        public void Rename(string name)
        {
            name = GuardHelper.AgainstNullOrWhiteSpace(
                value: name,
                errorFactory: DomainErrorsFactory.EmptyRoleName,
                trim: true);

            ValidateName(name);

            Name = name;
            NormalizedName = NormalizeName(name);
        }

        private static void ValidateName(string name)
        {
            if (name.Length > NameMaxLength)
                throw DomainErrorsFactory.InvalidRoleNameLength(
                    maxLength: NameMaxLength,
                    actualLength: name.Length,
                    propertyName: nameof(name));
        }

        private static string NormalizeName(string name)
        {
            return name.Trim()
               .ToUpperInvariant();
        }
    }
}
