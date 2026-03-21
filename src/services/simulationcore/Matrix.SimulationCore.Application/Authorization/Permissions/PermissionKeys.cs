using ContractPermissionKeys = Matrix.SimulationCore.Contracts.Authorization.Permissions.PermissionKeys;

namespace Matrix.SimulationCore.Application.Authorization.Permissions
{
    public static class PermissionKeys
    {
        public const string SimulationCoreScenariosCatalogRead = ContractPermissionKeys.SimulationCoreScenariosCatalogRead;

        public const string SimulationCoreClassicCityRead = ContractPermissionKeys.SimulationCoreClassicCityRead;
        public const string SimulationCoreClassicCityCreate = ContractPermissionKeys.SimulationCoreClassicCityCreate;
        public const string SimulationCoreClassicCityUpdate = ContractPermissionKeys.SimulationCoreClassicCityUpdate;
        public const string SimulationCoreClassicCityArchive = ContractPermissionKeys.SimulationCoreClassicCityArchive;
        public const string SimulationCoreClassicCityDelete = ContractPermissionKeys.SimulationCoreClassicCityDelete;

        public const string SimulationCoreClassicCityPopulationBootstrapRetry =
            ContractPermissionKeys.SimulationCoreClassicCityPopulationBootstrapRetry;

        public const string SimulationCoreSimulationRead = ContractPermissionKeys.SimulationCoreSimulationRead;
        public const string SimulationCoreSimulationControl = ContractPermissionKeys.SimulationCoreSimulationControl;
    }
}
