namespace Matrix.Identity.Application.Abstractions.Services
{
    public sealed record RefreshTokenDescriptor(
        string Token,
        string TokenHash,
        DateTime ExpiresAtUtc);

    public interface IRefreshTokenProvider
    {
        RefreshTokenDescriptor Generate(bool isPersistent);
        string ComputeHash(string token);
    }
}
