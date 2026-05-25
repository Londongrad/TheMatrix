using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Domain.Tests.ValueObjects
{
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

            Assert.Equal(
                expected: "device-1",
                actual: deviceInfo.DeviceId);
            Assert.Equal(
                expected: "Pixel",
                actual: deviceInfo.DeviceName);
            Assert.Equal(
                expected: string.Empty,
                actual: deviceInfo.UserAgent);
            Assert.Equal(
                expected: "127.0.0.1",
                actual: deviceInfo.IpAddress);
        }

        [Fact]
        public void Create_TrimsUserAgent()
        {
            var deviceInfo = DeviceInfo.Create(
                deviceId: "device-1",
                deviceName: "Pixel",
                userAgent: "  Mozilla/5.0  ",
                ipAddress: null);

            Assert.Equal(
                expected: "Mozilla/5.0",
                actual: deviceInfo.UserAgent);
        }

        [Fact]
        public void WithClientInfo_PreservesDeviceIdentity_AndUpdatesClientFields()
        {
            var deviceInfo = DeviceInfo.Create(
                deviceId: "device-1",
                deviceName: "Pixel",
                userAgent: "Mozilla/5.0",
                ipAddress: "127.0.0.1");

            DeviceInfo updated = deviceInfo.WithClientInfo(
                userAgent: "  Safari  ",
                ipAddress: "10.0.0.1");

            Assert.Equal(
                expected: deviceInfo.DeviceId,
                actual: updated.DeviceId);
            Assert.Equal(
                expected: deviceInfo.DeviceName,
                actual: updated.DeviceName);
            Assert.Equal(
                expected: "Safari",
                actual: updated.UserAgent);
            Assert.Equal(
                expected: "10.0.0.1",
                actual: updated.IpAddress);
        }

        [Fact]
        public void Create_WithInvalidDeviceId_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => DeviceInfo.Create(
                deviceId: "   ",
                deviceName: "Pixel",
                userAgent: "Mozilla/5.0",
                ipAddress: null));

            Assert.Equal(
                expected: "Identity.DeviceInfo.InvalidDeviceId",
                actual: exception.Code);
            Assert.Equal(
                expected: "deviceId",
                actual: exception.PropertyName);
        }
    }
}
