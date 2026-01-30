namespace Matrix.Population.Contracts.Models
{
    public sealed record class ResidentialBuildingSeedDto(
        Guid ResidentialBuildingId,
        Guid DistrictId,
        int ResidentCapacity);
}