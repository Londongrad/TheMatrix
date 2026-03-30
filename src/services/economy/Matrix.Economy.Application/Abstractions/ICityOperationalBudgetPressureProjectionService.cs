using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;

namespace Matrix.Economy.Application.Abstractions
{
    public interface ICityOperationalBudgetPressureProjectionService
    {
        Task<CityOperationalBudgetPressureDto> GetAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);
    }
}
