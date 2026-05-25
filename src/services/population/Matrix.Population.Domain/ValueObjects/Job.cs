using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed record class Job
    {
        public Job(
            WorkplaceId workplaceId,
            string title,
            CityAnchorId? workplaceAnchorId = null)
        {
            WorkplaceId = workplaceId;
            Title = GuardHelper.AgainstNullOrWhiteSpace(
                value: title,
                propertyName: nameof(Title));
            WorkplaceAnchorId = workplaceAnchorId;
        }

        public WorkplaceId WorkplaceId { get; }
        public string Title { get; }
        public CityAnchorId? WorkplaceAnchorId { get; }
    }
}
