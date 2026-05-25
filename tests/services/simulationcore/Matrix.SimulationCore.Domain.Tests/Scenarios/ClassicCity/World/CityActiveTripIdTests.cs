using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.World
{
    public sealed class CityActiveTripIdTests
    {
        [Fact]
        public void WhenGuidIsNotEmpty_CreatesIdentifier()
        {
            var value = Guid.Parse("30000000-0000-0000-0000-000000000100");
            var identifier = new CityActiveTripId(value);

            Assert.Equal(
                expected: value,
                actual: identifier.Value);
        }

        [Fact]
        public void WhenGuidIsEmpty_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new CityActiveTripId(Guid.Empty));

            Assert.Equal(
                expected: "Domain.Guard.EmptyGuid",
                actual: exception.Code);
            Assert.Equal(
                expected: "Value",
                actual: exception.PropertyName);
        }
    }
}
