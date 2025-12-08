namespace Matrix.Identity.Application.UseCases.Account.GetUserProfile
{
    public sealed class UserProfileResult
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = null!;
        public string Username { get; init; } = null!;
        public string? AvatarUrl { get; init; }
    }
}
