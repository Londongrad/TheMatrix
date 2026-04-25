using Matrix.Identity.Application.Tests.UseCases.Self;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.SendPasswordReset;

public sealed class SendPasswordResetCommandHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToOneTimeTokenDeliveryService()
    {
        var deliveryService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenDeliveryService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Auth.SendPasswordReset.SendPasswordResetCommandHandler(
            deliveryService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateSendPasswordResetCommand(
                email: "neo@matrix.local",
                ipAddress: "203.0.113.40",
                userAgent: "Mozilla/5.0 (password-reset)"),
            CancellationToken.None);

        var request = Assert.Single(deliveryService.PasswordResetRequests);
        Assert.Equal("neo@matrix.local", request.Email);
        Assert.Equal("203.0.113.40", request.IpAddress);
        Assert.Equal("Mozilla/5.0 (password-reset)", request.UserAgent);
    }
}
