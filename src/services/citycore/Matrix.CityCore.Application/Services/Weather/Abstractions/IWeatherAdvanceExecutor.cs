using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Services.Weather.Abstractions
{
    public interface IWeatherAdvanceExecutor
    {
        Task AdvanceAsync(
            CityId cityId,
            SimTime evaluatedAt,
            CancellationToken cancellationToken);
    }
}
