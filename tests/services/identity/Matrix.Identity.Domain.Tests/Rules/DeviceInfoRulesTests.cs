using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Rules;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Rules
{
    public sealed class DeviceInfoRulesTests
    {
        [Fact]
        public void Validate_TrimsAndReturnsDeviceIdAndDeviceName()
        {
            (string deviceId, string deviceName) = DeviceInfoRules.Validate(
                deviceId: "  device-1  ",
                deviceName: "  Pixel  ");

            Assert.Equal(
                expected: "device-1",
                actual: deviceId);
            Assert.Equal(
                expected: "Pixel",
                actual: deviceName);
        }

        [Fact]
        public void Validate_WithWhitespaceDeviceId_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => DeviceInfoRules.Validate(
                deviceId: "   ",
                deviceName: "Pixel"));

            Assert.Equal(
                expected: "Identity.DeviceInfo.InvalidDeviceId",
                actual: exception.Code);
            Assert.Equal(
                expected: "deviceId",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Validate_WithWhitespaceDeviceName_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => DeviceInfoRules.Validate(
                deviceId: "device-1",
                deviceName: "   "));

            Assert.Equal(
                expected: "Identity.DeviceInfo.InvalidDeviceName",
                actual: exception.Code);
            Assert.Equal(
                expected: "deviceName",
                actual: exception.PropertyName);
        }
    }
}
