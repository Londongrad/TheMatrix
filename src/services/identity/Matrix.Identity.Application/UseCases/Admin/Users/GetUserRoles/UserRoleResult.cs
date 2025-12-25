namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles
{
    public sealed class UserRoleResult
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsSystem { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}
