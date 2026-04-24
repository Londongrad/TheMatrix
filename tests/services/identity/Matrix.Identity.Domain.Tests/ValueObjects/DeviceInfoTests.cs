using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.ValueObjects;

public sealed class DeviceInfoTests
{
    [Fact]
    public void Create_TrimsValidatedFields_AndNormalizesNullUserAgent()
    {
        var deviceInfo = DeviceInfo.Create(
            deviceId: "  device-1  ",
            deviceName: "  Pixel  ",
            userAgent: null!,
            ipAddress: "127.0.0.1");

        Assert.Equal("device-1", deviceInfo.DeviceId);
        Assert.Equal("Pixel", deviceInfo.DeviceName);
        Assert.Equal(string.Empty, deviceInfo.UserAgent);
        Assert.Equal("127.0.0.1", deviceInfo.IpAddress);
    }

    [Fact]
    public void Create_TrimsUserAgent()
    {
        var deviceInfo = DeviceInfo.Create(
            deviceId: "device-1",
            deviceName: "Pixel",
            userAgent: "  Mozilla/5.0  ",
            ipAddress: null);

        Assert.Equal("Mozilla/5.0", deviceInfo.UserAgent);
    }

    [Fact]
    public void WithClientInfo_PreservesDeviceIdentity_AndUpdatesClientFields()
    {
        var deviceInfo = DeviceInfo.Create(
            deviceId: "device-1",
            deviceName: "Pixel",
            userAgent: "Mozilla/5.0",
            ipAddress: "127.0.0.1");

        var updated = deviceInfo.WithClientInfo(
            userAgent: "  Safari  ",
            ipAddress: "10.0.0.1");

        Assert.Equal(deviceInfo.DeviceId, updated.DeviceId);
        Assert.Equal(deviceInfo.DeviceName, updated.DeviceName);
        Assert.Equal("Safari", updated.UserAgent);
        Assert.Equal("10.0.0.1", updated.IpAddress);
    }

    [Fact]
    public void Create_WithInvalidDeviceId_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => DeviceInfo.Create(
            deviceId: "   ",
            deviceName: "Pixel",
            userAgent: "Mozilla/5.0",
            ipAddress: null));

        Assert.Equal("Identity.DeviceInfo.InvalidDeviceId", exception.Code);
        Assert.Equal("deviceId", exception.PropertyName);
    }
}
