using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Resources.Domain.Simulation;
using Xunit;

namespace Matrix.Resources.Domain.Tests.Simulation;

public sealed class SimulationHostIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_CreatesIdentifier()
    {
        Guid value = Guid.Parse("30000000-0000-0000-0000-000000000001");

        var id = new SimulationHostId(value);

        Assert.Equal(value, id.Value);
        Assert.Equal(value.ToString(), id.ToString());
    }

    [Fact]
    public void Constructor_WithEmptyGuid_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(() => new SimulationHostId(Guid.Empty));

        Assert.Equal("Resources.SimulationHost.Id.Empty", exception.Code);
    }
}
