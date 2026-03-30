using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RetryCityPopulationBootstrapProvisioning
{
    public enum RetryCityPopulationBootstrapProvisioningStatus
    {
        Provisioned = 1,
        NotFound = 2,
        NotAllowed = 3
    }

    public sealed record RetryCityPopulationBootstrapProvisioningResult(
        RetryCityPopulationBootstrapProvisioningStatus Status,
        CityProvisioningView? Provisioning)
    {
        public static RetryCityPopulationBootstrapProvisioningResult Provisioned(CityProvisioningView provisioning)
        {
            return new RetryCityPopulationBootstrapProvisioningResult(
                Status: RetryCityPopulationBootstrapProvisioningStatus.Provisioned,
                Provisioning: provisioning);
        }

        public static RetryCityPopulationBootstrapProvisioningResult NotFound()
        {
            return new RetryCityPopulationBootstrapProvisioningResult(
                Status: RetryCityPopulationBootstrapProvisioningStatus.NotFound,
                Provisioning: null);
        }

        public static RetryCityPopulationBootstrapProvisioningResult NotAllowed()
        {
            return new RetryCityPopulationBootstrapProvisioningResult(
                Status: RetryCityPopulationBootstrapProvisioningStatus.NotAllowed,
                Provisioning: null);
        }
    }
}
