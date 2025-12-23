namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage
{
    public sealed class UserListItemResult
    {
        public Guid Id { get; init; }
        public string? AvatarUrl { get; init; }
        public required string Email { get; init; }
        public required string Username { get; init; }
        public bool IsEmailConfirmed { get; init; }
        public bool IsLocked { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}
