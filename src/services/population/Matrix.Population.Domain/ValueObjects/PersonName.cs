using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class PersonName
    {
        public string FirstName { get; }
        public string LastName { get; }
        public string? Patronymic { get; }

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
