using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.World;

public sealed class CityActiveTripIdTests
{
    [Fact]
    public void WhenGuidIsNotEmpty_CreatesIdentifier()
    {
        var value = Guid.Parse("30000000-0000-0000-0000-000000000100");
        var identifier = new CityActiveTripId(value);

        Assert.Equal(value, identifier.Value);
    }

    [Fact]
    public void WhenGuidIsEmpty_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => new CityActiveTripId(Guid.Empty));

        Assert.Equal("Domain.Guard.EmptyGuid", exception.Code);
        Assert.Equal("Value", exception.PropertyName);
    }
}
