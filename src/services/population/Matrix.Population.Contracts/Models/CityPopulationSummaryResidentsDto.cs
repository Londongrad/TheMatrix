namespace Matrix.Population.Contracts.Models
{
    public sealed record class CityPopulationSummaryResidentsDto(
        int ResidentCount,
        int DeceasedCount,
        int HousedResidentCount,
        int HomelessResidentCount,
        int ChildCount,
        int YouthCount,
        int AdultCount,
        int SeniorCount,
        int EmployedCount,
        int StudentCount,
        int UnemployedCount,
        int RetiredCount,
        decimal? AverageHealth,
        decimal? AverageHappiness);
}
