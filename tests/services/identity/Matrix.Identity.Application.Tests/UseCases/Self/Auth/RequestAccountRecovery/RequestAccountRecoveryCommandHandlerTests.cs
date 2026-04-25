using Matrix.Identity.Application.Tests.UseCases.Self;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.RequestAccountRecovery;

public sealed class RequestAccountRecoveryCommandHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToOneTimeTokenDeliveryService()
    {
        var deliveryService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenDeliveryService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Auth.RequestAccountRecovery.RequestAccountRecoveryCommandHandler(
            deliveryService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateRequestAccountRecoveryCommand(
                email: "neo@matrix.local",
                ipAddress: "203.0.113.75",
                userAgent: "Mozilla/5.0 (account-recovery)"),
            CancellationToken.None);

        var request = Assert.Single(deliveryService.AccountRecoveryRequests);
        Assert.Equal("neo@matrix.local", request.Email);
        Assert.Equal("203.0.113.75", request.IpAddress);
        Assert.Equal("Mozilla/5.0 (account-recovery)", request.UserAgent);
    }
}
