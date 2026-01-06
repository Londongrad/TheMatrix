namespace Matrix.BuildingBlocks.Application.Authorization.Permissions
{
    public interface IRequirePermission
    {
        string PermissionKey { get; }
    }
}
