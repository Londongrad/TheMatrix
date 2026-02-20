namespace Matrix.BuildingBlocks.Application.Authorization.Permissions
{
    public interface IRequirePermissions
    {
        IReadOnlyCollection<string> PermissionKeys { get; }

        PermissionMatchMode PermissionMatchMode { get; }
    }
}
