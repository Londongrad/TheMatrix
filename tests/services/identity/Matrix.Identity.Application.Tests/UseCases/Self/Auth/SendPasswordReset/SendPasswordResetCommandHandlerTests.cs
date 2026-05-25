using Matrix.Identity.Application.UseCases.Self.Auth.SendPasswordReset;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.SendPasswordReset
{
    public sealed class SendPasswordResetCommandHandlerTests
    {
        [Fact]
        public async Task Handle_DelegatesToOneTimeTokenDeliveryService()
        {
            var deliveryService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenDeliveryService();
            var handler = new SendPasswordResetCommandHandler(deliveryService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateSendPasswordResetCommand(
                    email: "neo@matrix.local",
                    ipAddress: "203.0.113.40",
                    userAgent: "Mozilla/5.0 (password-reset)"),
                cancellationToken: CancellationToken.None);

            (string Email, string? IpAddress, string? UserAgent) request =
                Assert.Single(deliveryService.PasswordResetRequests);
            Assert.Equal(
                expected: "neo@matrix.local",
                actual: request.Email);
            Assert.Equal(
                expected: "203.0.113.40",
                actual: request.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (password-reset)",
                actual: request.UserAgent);
        }
    }
}
