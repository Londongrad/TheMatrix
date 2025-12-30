namespace Matrix.Identity.Application.Abstractions.Services.Validation
{
    public interface IRoleIdsValidator
    {
        Task ValidateExistAsync(
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken cancellationToken);
    }
}
