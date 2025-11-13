using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class Job
    {
        public WorkplaceId WorkplaceId { get; }
        public string Title { get; }

        public Job(WorkplaceId workplaceId, string title)
        {
            WorkplaceId = workplaceId;
            Title = GuardHelper.AgainstNullOrEmpty(title, nameof(Title));
        }
    }
}
