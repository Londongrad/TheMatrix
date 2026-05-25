using Matrix.Identity.Application.UseCases.Self.Account.SendEmailConfirmation;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.SendEmailConfirmation
{
    public sealed class SendEmailConfirmationCommandHandlerTests
    {
        [Fact]
        public async Task Handle_DelegatesToOneTimeTokenDeliveryService()
        {
            var deliveryService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenDeliveryService();
            var handler = new SendEmailConfirmationCommandHandler(deliveryService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateSendEmailConfirmationCommand(
                    email: "neo@matrix.local",
                    ipAddress: "203.0.113.74",
                    userAgent: "Mozilla/5.0 (email-confirmation)"),
                cancellationToken: CancellationToken.None);

            (string Email, string? IpAddress, string? UserAgent) request =
                Assert.Single(deliveryService.EmailConfirmationRequests);
            Assert.Equal(
                expected: "neo@matrix.local",
                actual: request.Email);
            Assert.Equal(
                expected: "203.0.113.74",
                actual: request.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (email-confirmation)",
                actual: request.UserAgent);
        }
    }
}
