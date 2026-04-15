namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.Common
{
    public sealed record CityActiveTripProgressDto(
        Guid DistrictId,
        Guid? RoadSegmentId,
        decimal SegmentProgressIndex,
        decimal PositionX,
        decimal PositionY);
}
