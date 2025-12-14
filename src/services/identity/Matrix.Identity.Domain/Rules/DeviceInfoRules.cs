using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.Rules
{
    public static class DeviceInfoRules
    {
        public static (string, string) Validate(
            string deviceId,
            string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw DomainErrorsFactory.InvalidDeviceId(nameof(deviceId));
            if (string.IsNullOrWhiteSpace(deviceName))
                throw DomainErrorsFactory.InvalidDeviceName(nameof(deviceName));

            return (deviceId.Trim(), deviceName.Trim());
        }
    }
}
