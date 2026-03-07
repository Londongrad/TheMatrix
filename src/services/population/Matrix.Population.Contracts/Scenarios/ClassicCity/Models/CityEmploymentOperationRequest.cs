namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record CityEmploymentOperationRequest(
        Guid ResidentId,
        string? JobTitle,
        DateOnly CurrentDate);
}
