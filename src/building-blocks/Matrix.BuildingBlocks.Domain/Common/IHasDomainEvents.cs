using Matrix.BuildingBlocks.Domain.Events;

namespace Matrix.BuildingBlocks.Domain.Common
{
    public interface IHasDomainEvents
    {
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

        void ClearDomainEvents();
    }
}
