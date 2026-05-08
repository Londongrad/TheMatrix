using Matrix.Identity.Infrastructure.Integration.Email;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Integration.Email;

public sealed class EmailSenderTests
{
    [Fact]
    public async Task SendPasswordReset_WhenLogOnlyMode_LogsEmailAndDoesNotThrow()
    {
        var logger = new TestLogger<EmailSender>();
        var sender = new EmailSender(
            options: Microsoft.Extensions.Options.Options.Create(
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
        Assert.Contains("neo@matrix.local", entry.Message);
        Assert.Contains("Reset your password", entry.Message);
        Assert.Contains("https://matrix.local/reset?token=abc", entry.Message);
    }
}
