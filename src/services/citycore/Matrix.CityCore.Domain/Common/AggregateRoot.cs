using Matrix.CityCore.Domain.Events;

namespace Matrix.CityCore.Domain.Common
{
    /// <summary>
    ///     Base type for aggregate roots with domain events support.
    /// </summary>
    public abstract class AggregateRoot<TId>(TId id)
        : Entity<TId>(id)
        where TId : notnull
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
