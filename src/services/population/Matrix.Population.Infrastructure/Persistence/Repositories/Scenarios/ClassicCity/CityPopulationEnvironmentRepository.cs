using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class CityPopulationEnvironmentRepository(PopulationDbContext dbContext)
        : ICityPopulationEnvironmentRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public Task<CityPopulationEnvironment?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.CityPopulationEnvironments.FirstOrDefaultAsync(
                predicate: x => x.CityId == cityId,
                cancellationToken: cancellationToken);
        }

        public async Task AddAsync(
            CityPopulationEnvironment environment,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationEnvironments.AddAsync(
                entity: environment,
                cancellationToken: cancellationToken);
        }

        public async Task<bool> UpsertAsync(
            CityPopulationEnvironment environment,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(environment);

            int affectedRows = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                sql: $"""
                      INSERT INTO "CityPopulationEnvironments"
                          ("CityId", "ClimateZone", "Hemisphere", "UtcOffsetMinutes", "CreatedAtUtc", "UpdatedAtUtc")
                      VALUES
                          ({environment.CityId.Value}, {(int)environment.ClimateZone}, {(int)environment.Hemisphere}, {environment.UtcOffsetMinutes}, {environment.CreatedAtUtc}, {environment.UpdatedAtUtc})
                      ON CONFLICT ("CityId") DO UPDATE
                      SET
                          "ClimateZone" = EXCLUDED."ClimateZone",
                          "Hemisphere" = EXCLUDED."Hemisphere",
                          "UtcOffsetMinutes" = EXCLUDED."UtcOffsetMinutes",
                          "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc"
                      WHERE "CityPopulationEnvironments"."UpdatedAtUtc" <= EXCLUDED."UpdatedAtUtc";
                      """,
                cancellationToken: cancellationToken);

            return affectedRows > 0;
        }

        public async Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationEnvironments
               .Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
