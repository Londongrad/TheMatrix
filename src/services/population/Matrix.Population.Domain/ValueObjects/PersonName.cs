using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed record class PersonName
    {
        public string FirstName { get; } = null!;
        public string LastName { get; } = null!;
        public string? Patronymic { get; }

        private PersonName() { }

        public PersonName(string firstName, string lastName, string? patronymic = null)
        {
            FirstName = GuardHelper.AgainstNullOrEmpty(firstName, nameof(FirstName));
            LastName = GuardHelper.AgainstNullOrEmpty(lastName, nameof(LastName));
            Patronymic = patronymic;
        }

        public static PersonName FromFullName(string fullName)
        {
            GuardHelper.AgainstNullOrEmpty(fullName, nameof(fullName));
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || parts.Length > 3)
            {
                throw new ArgumentException("Full name must consist of first name, last name, and optional patronymic.", nameof(fullName));
            }
            var lastName = parts[0];
            var firstName = parts[1];
            var patronymic = parts.Length == 3 ? parts[2] : null;
            return new PersonName(firstName, lastName, patronymic);
        }

        public override string ToString()
            => Patronymic is null
                ? $"{LastName} {FirstName}"
                : $"{LastName} {FirstName} {Patronymic}";
    }
}
