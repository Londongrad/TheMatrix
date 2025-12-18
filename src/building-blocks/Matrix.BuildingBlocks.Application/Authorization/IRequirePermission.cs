namespace Matrix.BuildingBlocks.Application.Authorization
{
    public interface IRequirePermission
    {
        string PermissionKey { get; }
    }
}
