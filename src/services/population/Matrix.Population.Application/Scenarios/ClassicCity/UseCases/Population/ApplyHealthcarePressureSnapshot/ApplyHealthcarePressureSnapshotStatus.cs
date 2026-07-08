namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyHealthcarePressureSnapshot;

public enum ApplyHealthcarePressureSnapshotStatus
{
    Applied = 0,
    Duplicate = 1,
    CityDeleted = 2,
    CityArchived = 3,
    Stale = 4
}
