using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Events;

namespace Matrix.Population.Domain.Services
{
    public sealed class MonthlyIncomeCalculator
    {
        public IReadOnlyCollection<IncomeEarned> CalculateMonthlyIncome(
            IReadOnlyCollection<Person> persons,
            int simulationMonth)
        {
            GuardHelper.AgainstNull(persons, nameof(persons));

            if (simulationMonth <= 0)
                throw new DomainValidationException("SimulationMonth must be positive.", nameof(simulationMonth));

            var results = new List<IncomeEarned>();

            foreach (var person in persons)
            {
                // Only employed persons with a job earn income
                if (person.EmploymentStatus != EmploymentStatus.Employed || person.Job is null)
                    continue;

                var montlyIncome = person.Job.CalculateMonthlyIncome();

                var income = new IncomeEarned(
                    PersonId: person.Id,
                    HouseholdId: person.HouseholdId,
                    DistrictId: person.DistrictId,
                    WorkplaceId: person.Job.WorkplaceId,
                    MonthlyIncome: montlyIncome,
                    SimulationMonth: simulationMonth);

                results.Add(income);
            }

            return results;
        }
    }
}
