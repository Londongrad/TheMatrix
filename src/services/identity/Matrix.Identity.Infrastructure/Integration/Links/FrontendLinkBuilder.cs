using Matrix.Identity.Application.Abstractions.Services;

namespace Matrix.Identity.Infrastructure.Integration.Links
{
    public class FrontendLinkBuilder : IFrontendLinkBuilder
    {
        public string BuildConfirmEmailLink(
            Guid userId,
            string rawToken)
        {
            throw new NotImplementedException();
        }
    }
}
