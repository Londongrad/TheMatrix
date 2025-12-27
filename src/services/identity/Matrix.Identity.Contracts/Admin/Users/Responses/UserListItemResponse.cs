namespace Matrix.Identity.Contracts.Admin.Users.Responses
{
    public sealed class UserListItemResponse
    {
        public Guid Id { get; init; }
        public string? AvatarUrl { get; set; }
        public required string Email { get; init; }
        public required string Username { get; init; }
        public bool IsEmailConfirmed { get; init; }
        public bool IsLocked { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}
