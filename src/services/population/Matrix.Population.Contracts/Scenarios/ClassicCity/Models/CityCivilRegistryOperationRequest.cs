namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityCivilRegistryOperationRequest(
        Guid FirstResidentId,
        Guid SecondResidentId,
        DateOnly CurrentDate);
}
