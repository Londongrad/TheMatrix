using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.World.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.World
{
    public sealed class CityActiveTripAdvanceExecutor(ICityActiveTripRepository tripRepository)
        : ICityActiveTripAdvanceExecutor
    {
        public async Task AdvanceAsync(
            CityId cityId,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc,
            long tickId,
            CancellationToken cancellationToken)
        {
            if (toSimTimeUtc <= fromSimTimeUtc)
                return;

            IReadOnlyList<CityActiveTrip> activeTrips =
                await tripRepository.ListActiveForUpdateByCityIdAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

            foreach (CityActiveTrip activeTrip in activeTrips)
                activeTrip.AdvanceTo(
                    toSimTimeUtc: toSimTimeUtc,
                    tickId: tickId);
        }
    }
}
