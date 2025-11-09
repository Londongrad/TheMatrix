namespace Matrix.Economy.Application.Abstractions
{
    public interface IEconomyUnitOfWork
    {
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
