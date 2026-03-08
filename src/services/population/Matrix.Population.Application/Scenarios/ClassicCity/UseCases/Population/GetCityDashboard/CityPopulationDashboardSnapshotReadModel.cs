namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard
{
    public sealed record CityPopulationDashboardSnapshotReadModel(
        Guid CityId,
        DateOnly SnapshotDate,
        int HouseholdCount,
        int HousedHouseholdCount,
        int HomelessHouseholdCount,
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
        decimal? AverageHappiness,
        decimal? AverageEnergy,
        decimal? AverageStress,
        decimal? AverageSocialNeed);
}
