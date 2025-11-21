namespace Matrix.Identity.Application.Abstractions
{
    public sealed record RefreshTokenDescriptor(
        string Token,
        string TokenHash,
        DateTime ExpiresAtUtc);

    public interface IRefreshTokenProvider
    {
        RefreshTokenDescriptor Generate();
        string ComputeHash(string token);
    }
}
