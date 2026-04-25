using Matrix.Identity.Application.Tests.UseCases.Self;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.SendEmailConfirmation;

public sealed class SendEmailConfirmationCommandHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToOneTimeTokenDeliveryService()
    {
        var deliveryService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenDeliveryService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.SendEmailConfirmation.SendEmailConfirmationCommandHandler(
            deliveryService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateSendEmailConfirmationCommand(
                email: "neo@matrix.local",
                ipAddress: "203.0.113.74",
                userAgent: "Mozilla/5.0 (email-confirmation)"),
            CancellationToken.None);

        var request = Assert.Single(deliveryService.EmailConfirmationRequests);
        Assert.Equal("neo@matrix.local", request.Email);
        Assert.Equal("203.0.113.74", request.IpAddress);
        Assert.Equal("Mozilla/5.0 (email-confirmation)", request.UserAgent);
    }
}
