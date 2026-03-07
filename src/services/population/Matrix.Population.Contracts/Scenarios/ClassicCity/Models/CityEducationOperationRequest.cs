namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record CityEducationOperationRequest(
        Guid ResidentId,
        string? TargetEducationLevel,
        DateOnly CurrentDate);
}
