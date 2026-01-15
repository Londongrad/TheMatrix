namespace Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions
{
    public interface IOutboxDispatcher
    {
        Task DispatchOnceAsync(CancellationToken cancellationToken);
    }
}
