using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    internal static class CityBudgetInitializationSupport
    {
        private const string CityBudgetByCityConstraintName = "IX_City_Budget_city_id";

        public static async Task<CityBudget> EnsureExistsAsync(
            Guid cityId,
            ICityBudgetRepository budgetRepository,
            IEconomyUnitOfWork unitOfWork,
            CancellationToken cancellationToken)
        {
            CityBudget? existingBudget = await budgetRepository.GetByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (existingBudget is not null)
                return existingBudget;

            var newBudget = new CityBudget(
                id: CityBudgetId.New(),
                cityId: cityId);
            budgetRepository.Add(newBudget);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return newBudget;
            }
            catch (DbUpdateException ex) when (IsConcurrentCityBudgetInitialization(ex))
            {
                CityBudget? concurrentBudget = await budgetRepository.GetByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

                if (concurrentBudget is not null)
                    return concurrentBudget;

                throw;
            }
        }

        private static bool IsConcurrentCityBudgetInitialization(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: CityBudgetByCityConstraintName
            };
        }
    }
}
