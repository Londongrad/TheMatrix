using ContractPermissionKeys =
    Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Authorization.Permissions.PermissionKeys;

namespace Matrix.SimulationSystems.Application.Authorization.Permissions
{
    public static class PermissionKeys
    {
        public const string SimulationSystemsClassicCityRead = ContractPermissionKeys.SimulationSystemsClassicCityRead;

        public const string SimulationSystemsClassicCityManage =
            ContractPermissionKeys.SimulationSystemsClassicCityManage;
    }
}
