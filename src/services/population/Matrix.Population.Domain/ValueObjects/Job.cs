using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed record class Job
    {
        public Job(WorkplaceId workplaceId, string title)
        {
            WorkplaceId = workplaceId;
            Title = GuardHelper.AgainstNullOrEmpty(value: title, propertyName: nameof(Title));
        }

        public WorkplaceId WorkplaceId { get; }
        public string Title { get; }
    }
}
