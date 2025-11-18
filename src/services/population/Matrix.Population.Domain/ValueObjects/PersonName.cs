using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class PersonName
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

        public override string ToString()
            => Patronymic is null
                ? $"{LastName} {FirstName}"
                : $"{LastName} {FirstName} {Patronymic}";
    }
}
