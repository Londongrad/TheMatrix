namespace Matrix.Identity.Application.Abstractions.Services.Validation
{
    public interface IPermissionKeysValidator
    {
        Task ValidateAsync(
            IReadOnlyCollection<string> permissionKeys,
            CancellationToken cancellationToken);
    }
}
