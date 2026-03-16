using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.Common
{
    internal static class ClassicCityWidowhoodSupport
    {
        public static bool TryRegisterWidowhood(
            Person deceased,
            Person? spouse,
            MarriageDomainService marriageDomainService)
        {
            if (deceased.MaritalStatus != MaritalStatus.Married || deceased.SpouseId is null)
                return false;

            if (deceased.LifeStatus != LifeStatus.Deceased || spouse is null || !spouse.IsAlive)
                return false;

            if (spouse.MaritalStatus != MaritalStatus.Married || spouse.SpouseId != deceased.Id)
                return false;

            marriageDomainService.RegisterWidowhood(
                widow: spouse,
                deceased: deceased);

            return true;
        }

        public static bool TryRegisterWidowhood(
            Person deceased,
            IReadOnlyDictionary<PersonId, Person> residentsById,
            MarriageDomainService marriageDomainService)
        {
            if (deceased.SpouseId is not
                { } spouseId)
                return false;

            return residentsById.TryGetValue(
                       key: spouseId,
                       value: out Person? spouse) &&
                   TryRegisterWidowhood(
                       deceased: deceased,
                       spouse: spouse,
                       marriageDomainService: marriageDomainService);
        }
    }
}
