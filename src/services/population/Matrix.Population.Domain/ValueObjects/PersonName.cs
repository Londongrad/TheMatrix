using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Errors;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed record class PersonName
    {
        private PersonName()
        {
        }

        public PersonName(string firstName, string lastName, string? patronymic = null)
        {
            FirstName = GuardHelper.AgainstNullOrEmpty(value: firstName, propertyName: nameof(FirstName));
            LastName = GuardHelper.AgainstNullOrEmpty(value: lastName, propertyName: nameof(LastName));
            Patronymic = patronymic;
        }

        public string FirstName { get; } = null!;
        public string LastName { get; } = null!;
        public string? Patronymic { get; }

        public static PersonName FromFullName(string fullName)
        {
            GuardHelper.AgainstNullOrEmpty(value: fullName, propertyName: nameof(fullName));
            string[] parts = fullName.Split(separator: ' ', options: StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || parts.Length > 3) throw DomainErrorsFactory.InvalidFullName(nameof(fullName));
            string lastName = parts[0];
            string firstName = parts[1];
            string? patronymic = parts.Length == 3 ? parts[2] : null;
            return new PersonName(firstName: firstName, lastName: lastName, patronymic: patronymic);
        }

        public override string ToString()
            => Patronymic is null
                ? $"{LastName} {FirstName}"
                : $"{LastName} {FirstName} {Patronymic}";
    }
}
