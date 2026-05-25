using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Simulation
{
    public sealed class DomainIdentifierTests
    {
        private const string EmptyGuidErrorCode = "Domain.Guard.EmptyGuid";

        [Fact]
        public void CityId_WhenGuidIsNotEmpty_CreatesIdentifier()
        {
            var guid = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var id = new CityId(guid);

            Assert.Equal(
                expected: guid,
                actual: id.Value);
        }

        [Fact]
        public void CityId_WhenGuidIsEmpty_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new CityId(Guid.Empty));

            Assert.Equal(
                expected: EmptyGuidErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void SimulationId_WhenGuidIsNotEmpty_CreatesIdentifier()
        {
            var guid = Guid.Parse("22222222-2222-2222-2222-222222222222");

            var id = new SimulationId(guid);

            Assert.Equal(
                expected: guid,
                actual: id.Value);
        }

        [Fact]
        public void SimulationId_WhenGuidIsEmpty_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new SimulationId(Guid.Empty));

            Assert.Equal(
                expected: EmptyGuidErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void SimulationHostId_WhenGuidIsNotEmpty_CreatesIdentifier()
        {
            var guid = Guid.Parse("33333333-3333-3333-3333-333333333333");

            var id = new SimulationHostId(guid);

            Assert.Equal(
                expected: guid,
                actual: id.Value);
        }

        [Fact]
        public void SimulationHostId_WhenGuidIsEmpty_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => new SimulationHostId(Guid.Empty));

            Assert.Equal(
                expected: EmptyGuidErrorCode,
                actual: exception.Code);
        }
    }
}
