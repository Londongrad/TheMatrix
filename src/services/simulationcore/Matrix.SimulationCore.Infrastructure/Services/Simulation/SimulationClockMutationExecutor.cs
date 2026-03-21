using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Matrix.SimulationCore.Infrastructure.Services.Simulation
{
    public sealed class SimulationClockMutationExecutor(
        SimulationCoreDbContext dbContext,
        ISimulationHostReadRepository simulationHostRepository,
        ILogger<SimulationClockMutationExecutor> logger) : ISimulationClockMutationExecutor
    {
        private const int MaxAttempts = 3;

        public async Task<bool> ExecuteAsync(
            SimulationId simulationId,
            Action<SimulationClock> mutate,
            CancellationToken cancellationToken,
            bool allowArchivedHost = false)
        {
            DbUpdateConcurrencyException? lastException = null;

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    SimulationHost? host = await simulationHostRepository.GetBySimulationIdAsync(
                        simulationId: simulationId,
                        cancellationToken: cancellationToken);

                    if (host is null)
                        return false;

                    if (host.IsArchived && !allowArchivedHost)
                        throw new MatrixApplicationException(
                            code: "CityCore.Simulation.ArchivedHost",
                            message: "Archived simulation hosts are read-only. Simulation controls are unavailable.",
                            errorType: ApplicationErrorType.Conflict);

                    CityId cityId = new(host.HostId.Value);

                    SimulationClock? clock = await dbContext.SimulationClocks.SingleOrDefaultAsync(
                        predicate: x => x.Id == cityId,
                        cancellationToken: cancellationToken);

                    if (clock is null)
                        return false;

                    mutate(clock);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    return true;
                }
                catch (DbUpdateConcurrencyException ex) when (attempt < MaxAttempts)
                {
                    lastException = ex;

                    logger.LogWarning(
                        exception: ex,
                        message:
                        "Concurrent update detected for simulation clock {SimulationId}. Retrying attempt {Attempt} of {MaxAttempts}.",
                        args:
                        [
                            simulationId.Value,
                            attempt + 1,
                            MaxAttempts
                        ]);

                    dbContext.ChangeTracker.Clear();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    lastException = ex;
                    dbContext.ChangeTracker.Clear();
                    break;
                }
            }

            logger.LogWarning(
                exception: lastException,
                message:
                "Simulation clock {SimulationId} could not be updated after {MaxAttempts} attempts because it kept changing concurrently.",
                args:
                [
                    simulationId.Value,
                    MaxAttempts
                ]);

            throw new MatrixApplicationException(
                code: "CityCore.SimulationClockConflict",
                message: "Simulation clock was updated concurrently. Please retry the action.",
                errorType: ApplicationErrorType.Conflict,
                innerException: lastException);
        }
    }
}
