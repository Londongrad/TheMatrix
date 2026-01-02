namespace Matrix.Identity.Application.UseCases.Admin.Roles.RenameRole
{
    public sealed class RoleRenamedResult
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsSystem { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}
