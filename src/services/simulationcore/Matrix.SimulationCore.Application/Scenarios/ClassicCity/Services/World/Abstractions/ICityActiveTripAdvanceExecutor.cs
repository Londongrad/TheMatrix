using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.World.Abstractions
{
    public interface ICityActiveTripAdvanceExecutor
    {
        Task AdvanceAsync(
            CityId cityId,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc,
            long tickId,
            CancellationToken cancellationToken);
    }
}
