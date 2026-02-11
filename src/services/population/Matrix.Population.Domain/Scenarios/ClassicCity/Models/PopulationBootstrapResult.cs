using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record PopulationBootstrapResult(
        IReadOnlyCollection<Household> Households,
        IReadOnlyCollection<ClassicCityHouseholdPlacement> HouseholdPlacements,
        IReadOnlyCollection<Person> Persons);
}
