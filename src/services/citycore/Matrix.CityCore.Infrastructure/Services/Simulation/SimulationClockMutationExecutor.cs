using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.CityCore.Application.Services.Simulation;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Matrix.CityCore.Infrastructure.Services.Simulation
{
    public sealed class SimulationClockMutationExecutor(
        CityCoreDbContext dbContext,
        ISimulationHostResolver simulationHostResolver,
        ILogger<SimulationClockMutationExecutor> logger) : ISimulationClockMutationExecutor
    {
        private const int MaxAttempts = 3;

        public async Task<bool> ExecuteAsync(
            SimulationId simulationId,
            Action<SimulationClock> mutate,
            CancellationToken cancellationToken,
            bool allowArchivedHost = false)
        {
            CityId cityId = new(simulationId.Value);
            DbUpdateConcurrencyException? lastException = null;

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    SimulationHostDescriptor? host = await simulationHostResolver.GetBySimulationIdAsync(
                        simulationId: simulationId,
                        cancellationToken: cancellationToken);

                    if (host is null)
                        return false;

                    if (!host.IsActive && !allowArchivedHost)
                    {
                        throw new MatrixApplicationException(
                            code: host.IsArchived
                                ? "CityCore.Simulation.ArchivedHost"
                                : "CityCore.Simulation.HostNotActive",
                            message: host.IsArchived
                                ? "Archived simulation hosts are read-only. Simulation controls are unavailable."
                                : "Only active simulation hosts can be controlled. Provisioning hosts stay paused until bootstrap finishes.",
                            errorType: ApplicationErrorType.Conflict);
                    }

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
