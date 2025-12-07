using Matrix.Identity.Domain.Rules;

namespace Matrix.Identity.Domain.ValueObjects
{
    public sealed record DeviceInfo
    {
        private DeviceInfo()
        {
        }

        private DeviceInfo(string deviceId, string deviceName, string userAgent, string? ipAddress)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
            UserAgent = userAgent;
            IpAddress = ipAddress;
        }

        public string DeviceId { get; } = null!;
        public string DeviceName { get; } = null!;
        public string UserAgent { get; } = null!;
        public string? IpAddress { get; }

        public static DeviceInfo Create(
            string deviceId,
            string deviceName,
            string userAgent,
            string? ipAddress)
        {
            (string deviceIdTrimmed, string deviceNameTrimmed) =
                DeviceInfoRules.Validate(deviceId: deviceId, deviceName: deviceName);

            userAgent ??= string.Empty;

            return new DeviceInfo(
                deviceId: deviceIdTrimmed,
                deviceName: deviceNameTrimmed,
                userAgent: userAgent.Trim(),
                ipAddress: ipAddress);
        }

        /// <summary>
        ///     Creates a new instance with the same device id/name but updated client info.
        /// </summary>
        public DeviceInfo WithClientInfo(string userAgent, string? ipAddress)
            => Create(deviceId: DeviceId, deviceName: DeviceName, userAgent: userAgent, ipAddress: ipAddress);
    }
}
