namespace Matrix.Identity.Application.Abstractions.Services
{
    public interface IFrontendLinkBuilder
    {
        string BuildConfirmEmailLink(
            Guid userId,
            string rawToken);
    }
}
