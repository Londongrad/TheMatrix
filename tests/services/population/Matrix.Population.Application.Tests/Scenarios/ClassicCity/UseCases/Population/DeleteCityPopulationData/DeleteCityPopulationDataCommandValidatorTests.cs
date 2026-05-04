using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.DeleteCityPopulationData;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.DeleteCityPopulationData;

public sealed class DeleteCityPopulationDataCommandValidatorTests
{
    private readonly DeleteCityPopulationDataCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(CreateCommand());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidFields_ReturnsErrors()
    {
        var result = _validator.Validate(CreateCommand() with
        {
            CityId = Guid.Empty,
            IntegrationMessageId = Guid.Empty,
            ConsumerName = "",
            DeletedAtUtc = new DateTimeOffset(2048, 5, 4, 14, 0, 0, TimeSpan.FromHours(3))
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
        Assert.Contains(result.Errors, x => x.PropertyName == "IntegrationMessageId");
        Assert.Contains(result.Errors, x => x.PropertyName == "ConsumerName");
        Assert.Contains(result.Errors, x => x.PropertyName == "DeletedAtUtc");
    }

    private static DeleteCityPopulationDataCommand CreateCommand()
    {
        return new DeleteCityPopulationDataCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ConsumerName: "population-delete",
            DeletedAtUtc: new DateTimeOffset(2048, 5, 4, 14, 0, 0, TimeSpan.Zero));
    }
}
