namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class ResidentialBuildingSeedDto(
        Guid ResidentialBuildingId,
        Guid DistrictId,
        int ResidentCapacity);
}
