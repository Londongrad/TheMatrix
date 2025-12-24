namespace Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile
{
    public sealed class MyProfileResult
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = null!;
        public string Username { get; init; } = null!;
        public string? AvatarUrl { get; init; }
    }
}
