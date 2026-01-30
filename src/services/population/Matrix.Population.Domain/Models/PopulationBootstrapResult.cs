using Matrix.Population.Domain.Entities;

namespace Matrix.Population.Domain.Models
{
    public sealed record PopulationBootstrapResult(
        IReadOnlyCollection<Household> Households,
        IReadOnlyCollection<Person> Persons);
}
