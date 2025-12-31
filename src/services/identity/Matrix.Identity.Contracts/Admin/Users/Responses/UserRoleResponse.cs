namespace Matrix.Identity.Contracts.Admin.Users.Responses
{
    public sealed class UserRoleResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsSystem { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}
