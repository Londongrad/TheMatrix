using FluentValidation.Results;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.Common;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.GetCityStockpiles;
using Matrix.Resources.Application.Tests.TestSupport;
using Xunit;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.GetCityStockpiles
{
    public sealed class GetCityStockpilesTests
    {
        [Fact]
        public void Validator_RejectsEmptyCityId()
        {
            var validator = new GetCityStockpilesQueryValidator();

            ValidationResult? result = validator.Validate(new GetCityStockpilesQuery(Guid.Empty));

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task Handler_ReturnsNullWhenStateIsMissing()
        {
            var repository = new FakeCityStockpileRepository();
            var handler = new GetCityStockpilesQueryHandler(repository);

            CityStockpilesDto? dto = await handler.Handle(
                request: new GetCityStockpilesQuery(CityId),
                cancellationToken: CancellationToken.None);

            Assert.Null(dto);
            Assert.Equal(
                expected: CreateHostId(),
                actual: repository.RequestedSimulationHostId);
        }

        [Fact]
        public async Task Handler_MapsDomainStateToDto()
        {
            var repository = new FakeCityStockpileRepository
            {
                State = CreateState()
            };
            repository.State.MarkTickApplied(9);
            var handler = new GetCityStockpilesQueryHandler(repository);

            CityStockpilesDto? dto = await handler.Handle(
                request: new GetCityStockpilesQuery(CityId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(dto);
            Assert.Equal(
                expected: CityId,
                actual: dto.CityId);
            Assert.Equal(
                expected: 9,
                actual: dto.EffectiveTickId);
            Assert.Equal(
                expected: repository.State.SupplyStressIndex,
                actual: dto.SupplyStressIndex);
            Assert.Equal(
                expected: repository.State.Fuel.StockLevelIndex,
                actual: dto.Fuel.StockLevelIndex);
        }
    }
}
