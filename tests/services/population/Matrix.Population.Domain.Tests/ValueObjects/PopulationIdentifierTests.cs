using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.ValueObjects
{
    public sealed class PopulationIdentifierTests
    {
        [Fact]
        public void PersonIdFrom_WhenGuidIsValid_PreservesValue()
        {
            var value = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var personId = PersonId.From(value);

            Assert.Equal(
                expected: value,
                actual: personId.Value);
        }

        [Fact]
        public void PopulationIdsFrom_WhenGuidIsEmpty_ThrowDomainException()
        {
            Assert.Throws<DomainException>(() => PersonId.From(Guid.Empty));
            Assert.Throws<DomainException>(() => HouseholdId.From(Guid.Empty));
            Assert.Throws<DomainException>(() => WorkplaceId.From(Guid.Empty));
            Assert.Throws<DomainException>(() => LocationAnchorId.From(Guid.Empty));
            Assert.Throws<DomainException>(() => CityAnchorId.From(Guid.Empty));
        }

        [Fact]
        public void CityAnchorId_ConvertsToAndFromCommonLocationAnchorId()
        {
            var value = Guid.Parse("22222222-2222-2222-2222-222222222222");
            CityAnchorId cityAnchorId = CityAnchorId.From(value);

            LocationAnchorId locationAnchorId = cityAnchorId;
            CityAnchorId roundTripCityAnchorId = locationAnchorId;

            Assert.Equal(
                expected: value,
                actual: locationAnchorId.Value);
            Assert.Equal(
                expected: cityAnchorId,
                actual: roundTripCityAnchorId);
        }
    }
}
