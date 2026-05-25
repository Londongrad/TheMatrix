using Matrix.Identity.Application.UseCases.Self.Auth.RequestAccountRecovery;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.RequestAccountRecovery
{
    public sealed class RequestAccountRecoveryCommandHandlerTests
    {
        [Fact]
        public async Task Handle_DelegatesToOneTimeTokenDeliveryService()
        {
            var deliveryService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenDeliveryService();
            var handler = new RequestAccountRecoveryCommandHandler(deliveryService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateRequestAccountRecoveryCommand(
                    email: "neo@matrix.local",
                    ipAddress: "203.0.113.75",
                    userAgent: "Mozilla/5.0 (account-recovery)"),
                cancellationToken: CancellationToken.None);

            (string Email, string? IpAddress, string? UserAgent) request =
                Assert.Single(deliveryService.AccountRecoveryRequests);
            Assert.Equal(
                expected: "neo@matrix.local",
                actual: request.Email);
            Assert.Equal(
                expected: "203.0.113.75",
                actual: request.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (account-recovery)",
                actual: request.UserAgent);
        }
    }
}
