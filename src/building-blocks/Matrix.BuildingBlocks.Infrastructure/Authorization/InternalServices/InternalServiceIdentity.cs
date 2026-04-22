namespace Matrix.BuildingBlocks.Infrastructure.Authorization.InternalServices
{
    public readonly record struct InternalServiceIdentity(
        Guid SubjectId,
        string ServiceName);
}
