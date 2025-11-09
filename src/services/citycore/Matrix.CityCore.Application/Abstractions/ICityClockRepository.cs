using Matrix.CityCore.Domain.Aggregates;

namespace Matrix.CityCore.Application.Abstractions
{
    public interface ICityClockRepository
    {
        Task<CityClock?> GetAsync(CancellationToken cancellationToken = default);
        void Add(CityClock clock);
    }
}
