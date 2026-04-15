namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views
{
    public sealed record CityActiveTripProgressView(
        Guid DistrictId,
        Guid? RoadSegmentId,
        decimal SegmentProgressIndex,
        decimal PositionX,
        decimal PositionY);
}
