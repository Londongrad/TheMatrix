using Matrix.Identity.Infrastructure.Integration.Email;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.Identity.Infrastructure.Tests.Integration.Email
{
    public sealed class EmailSenderTests
    {
        [Fact]
        public async Task SendPasswordReset_WhenLogOnlyMode_LogsEmailAndDoesNotThrow()
        {
            var logger = new TestLogger<EmailSender>();
            var sender = new EmailSender(
                options: Options.Create(
                    new EmailOptions
                    {
                        DeliveryMode = EmailDeliveryMode.LogOnly,
                        FromEmail = "noreply@matrix.local",
                        FromName = "The Matrix"
                    }),
                logger: logger);

            await sender.SendPasswordReset(
                toEmail: "neo@matrix.local",
                resetLink: "https://matrix.local/reset?token=abc",
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Contains(
                expectedSubstring: "neo@matrix.local",
                actualString: entry.Message);
            Assert.Contains(
                expectedSubstring: "Reset your password",
                actualString: entry.Message);
            Assert.Contains(
                expectedSubstring: "https://matrix.local/reset?token=abc",
                actualString: entry.Message);
        }
    }
}
