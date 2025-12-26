namespace Matrix.Identity.Contracts.Admin.Users.Responses
{
    public sealed class UserDetailsResponse
    {
        public Guid Id { get; init; }
        public string? AvatarUrl { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public bool IsEmailConfirmed { get; init; }
        public bool IsLocked { get; init; }
        public int PermissionsVersion { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}
