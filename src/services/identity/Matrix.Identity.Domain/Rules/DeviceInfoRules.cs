using Matrix.BuildingBlocks.Domain;
using Matrix.Identity.Domain.Errors;

namespace Matrix.Identity.Domain.Rules
{
    public static class DeviceInfoRules
    {
        public static (string, string) Validate(
            string deviceId,
            string deviceName)
        {
            string id = GuardHelper.AgainstNullOrWhiteSpace(
                value: deviceId,
                errorFactory: DomainErrorsFactory.InvalidDeviceId);

            string name = GuardHelper.AgainstNullOrWhiteSpace(
                value: deviceName,
                errorFactory: DomainErrorsFactory.InvalidDeviceName);

            return (id, name);
        }
    }
}
