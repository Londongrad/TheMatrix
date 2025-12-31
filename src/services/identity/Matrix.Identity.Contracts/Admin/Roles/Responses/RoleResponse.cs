namespace Matrix.Identity.Contracts.Admin.Roles.Responses
{
    public sealed class RoleResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsSystem { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}
