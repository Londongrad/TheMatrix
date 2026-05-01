using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Economy.Domain.ValueObjects;
using Xunit;

namespace Matrix.Economy.Domain.Tests.ValueObjects;

public sealed class CityBudgetIdTests
{
    [Fact]
    public void Constructor_WhenGuidIsValid_PreservesValue()
    {
        Guid value = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var budgetId = new CityBudgetId(value);

        Assert.Equal(value, budgetId.Value);
    }

    [Fact]
    public void Constructor_WhenGuidIsEmpty_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(() => new CityBudgetId(Guid.Empty));

        Assert.Equal("Domain.Guard.EmptyGuid", exception.Code);
    }

    [Fact]
    public void New_WhenCalled_ReturnsNonEmptyGuid()
    {
        CityBudgetId budgetId = CityBudgetId.New();

        Assert.NotEqual(Guid.Empty, budgetId.Value);
    }
}
