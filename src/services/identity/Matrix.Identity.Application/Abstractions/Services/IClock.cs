namespace Matrix.Identity.Application.Abstractions.Services
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
