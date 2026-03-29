namespace Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices
{
    public interface IInternalServiceJwtIssuer
    {
        string Issue(Guid subjectId);
    }
}
