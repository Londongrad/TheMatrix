using Matrix.BuildingBlocks.Domain.Common;
using Matrix.BuildingBlocks.Domain.Events;

namespace Matrix.BuildingBlocks.Application.Events
{
    public static class DomainEventDispatchHelper
    {
        /// <summary>
        ///     Applies the explicit domain-event convention:
        ///     publish the current aggregate events inside the caller's transaction,
        ///     then clear the aggregate so the same events are not published twice.
        /// </summary>
        public static async Task PublishAndClearAsync(
            IHasDomainEvents source,
            Func<IReadOnlyCollection<IDomainEvent>, CancellationToken, Task> publish,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(publish);

            if (source.DomainEvents.Count == 0)
                return;

            IDomainEvent[] domainEvents = [.. source.DomainEvents];

            await publish(
                domainEvents,
                cancellationToken);

            source.ClearDomainEvents();
        }
    }
}
