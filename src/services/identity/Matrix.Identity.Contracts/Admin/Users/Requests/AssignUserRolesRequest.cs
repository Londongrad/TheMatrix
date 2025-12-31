namespace Matrix.Identity.Contracts.Admin.Users.Requests
{
    public sealed class AssignUserRolesRequest
    {
        public required IReadOnlyCollection<Guid> RoleIds { get; init; }
    }
}
