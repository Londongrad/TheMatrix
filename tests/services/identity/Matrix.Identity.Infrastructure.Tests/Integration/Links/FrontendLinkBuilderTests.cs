using Matrix.Identity.Infrastructure.Integration.Links;
using Microsoft.Extensions.Options;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Integration.Links
{
    public sealed class FrontendLinkBuilderTests
    {
        [Fact]
        public void BuildConfirmEmailLink_BuildsAbsoluteUrlAndEscapesQueryParameters()
        {
            var builder = new FrontendLinkBuilder(CreateFrontendLinksOptions());
            var userId = Guid.Parse("8b6fc8cb-a2a4-491d-9450-309c4e5d52f0");

            string link = builder.BuildConfirmEmailLink(
                userId: userId,
                rawToken: "raw+/=?token");

            Assert.Equal(
                expected: $"https://matrix.local/app/confirm-email?userId={userId:D}&token=raw%2B%2F%3D%3Ftoken",
                actual: link);
        }

        [Fact]
        public void BuildResetPasswordLink_WhenPathDoesNotStartWithSlash_StillBuildsCorrectUrl()
        {
            var builder = new FrontendLinkBuilder(
                Options.Create(
                    new FrontendLinksOptions
                    {
                        BaseUrl = "https://matrix.local",
                        ResetPasswordPath = "reset-password"
                    }));

            string link = builder.BuildResetPasswordLink(
                userId: Guid.Parse("9a2cc7e8-1444-4852-9abc-e2aa5e11afb4"),
                rawToken: "abc");

            Assert.Equal(
                expected: "https://matrix.local/reset-password?userId=9a2cc7e8-1444-4852-9abc-e2aa5e11afb4&token=abc",
                actual: link);
        }
    }
}
