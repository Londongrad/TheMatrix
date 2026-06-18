using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed record class Job
    {
        public Job(
            WorkplaceId workplaceId,
            string title,
            LocationAnchorId? workplaceAnchorId = null)
        {
            WorkplaceId = workplaceId;
            Title = GuardHelper.AgainstNullOrWhiteSpace(
                value: title,
                propertyName: nameof(Title));
            WorkplaceAnchorId = workplaceAnchorId;
        }

        public WorkplaceId WorkplaceId { get; }
        public string Title { get; }
        public LocationAnchorId? WorkplaceAnchorId { get; }
    }
}
