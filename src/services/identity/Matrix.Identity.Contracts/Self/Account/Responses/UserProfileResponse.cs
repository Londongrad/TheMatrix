namespace Matrix.Identity.Contracts.Self.Account.Responses
{
    public class UserProfileResponse
    {
        public required Guid UserId { get; init; }

        public required string Email { get; init; }

        public required string Username { get; init; }

        public string? AvatarUrl { get; set; }

        public required string[] EffectivePermissions { get; init; }

        public required int PermissionsVersion { get; init; }
    }
}
