namespace Matrix.CityCore.Application.Abstractions
{
    public interface ICityCoreUnitOfWork
    {
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
