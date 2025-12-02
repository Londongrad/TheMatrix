using Matrix.Identity.Domain.Rules;

namespace Matrix.Identity.Domain.ValueObjects
{
    public sealed record DeviceInfo
    {
        public string DeviceId { get; } = null!;
        public string DeviceName { get; } = null!;
        public string UserAgent { get; } = null!;
        public string? IpAddress { get; }

        private DeviceInfo() { }

        private DeviceInfo(string deviceId, string deviceName, string userAgent, string? ipAddress)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
            UserAgent = userAgent;
            IpAddress = ipAddress;
        }

        public static DeviceInfo Create(
            string deviceId,
            string deviceName,
            string userAgent,
            string? ipAddress)
        {
            var (deviceIdTrimmed, deviceNameTrimmed) = DeviceInfoRules.Validate(deviceId, deviceName);

            userAgent ??= string.Empty;

            return new DeviceInfo(
                deviceIdTrimmed,
                deviceNameTrimmed,
                userAgent.Trim(),
                ipAddress);
        }

        /// <summary>
        /// Creates a new instance with the same device id/name but updated client info.
        /// </summary>
        public DeviceInfo WithClientInfo(string userAgent, string? ipAddress)
            => Create(DeviceId, DeviceName, userAgent, ipAddress);
    }
}
