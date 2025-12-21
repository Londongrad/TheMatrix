namespace Matrix.Identity.Contracts.Account.Responses
{
    public class UserProfileResponse
    {
        public required Guid UserId { get; init; }

        public required string Email { get; init; }

        public required string Username { get; init; }

        public string? AvatarUrl { get; set; }
    }
}
