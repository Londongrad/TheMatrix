namespace Matrix.BuildingBlocks.Application.Abstractions
{
    public interface ICurrentUserContext
    {
        bool IsAuthenticated { get; }
        Guid? UserId { get; }

        IReadOnlySet<string> Permissions { get; }
    }
}
