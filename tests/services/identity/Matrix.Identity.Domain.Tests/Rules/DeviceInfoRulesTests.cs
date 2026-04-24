using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Rules;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Rules;

public sealed class DeviceInfoRulesTests
{
    [Fact]
    public void Validate_TrimsAndReturnsDeviceIdAndDeviceName()
    {
        var (deviceId, deviceName) = DeviceInfoRules.Validate(
            deviceId: "  device-1  ",
            deviceName: "  Pixel  ");

        Assert.Equal("device-1", deviceId);
        Assert.Equal("Pixel", deviceName);
    }

    [Fact]
    public void Validate_WithWhitespaceDeviceId_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => DeviceInfoRules.Validate(
            deviceId: "   ",
            deviceName: "Pixel"));

        Assert.Equal("Identity.DeviceInfo.InvalidDeviceId", exception.Code);
        Assert.Equal("deviceId", exception.PropertyName);
    }

    [Fact]
    public void Validate_WithWhitespaceDeviceName_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => DeviceInfoRules.Validate(
            deviceId: "device-1",
            deviceName: "   "));

        Assert.Equal("Identity.DeviceInfo.InvalidDeviceName", exception.Code);
        Assert.Equal("deviceName", exception.PropertyName);
    }
}
