using ContractPermissionKeys = Matrix.CityCore.Contracts.Authorization.Permissions.PermissionKeys;

namespace Matrix.CityCore.Application.Authorization.Permissions
{
    public static class PermissionKeys
    {
        public const string CityCoreScenariosCatalogRead = ContractPermissionKeys.CityCoreScenariosCatalogRead;

        public const string CityCoreClassicCityRead = ContractPermissionKeys.CityCoreClassicCityRead;
        public const string CityCoreClassicCityCreate = ContractPermissionKeys.CityCoreClassicCityCreate;
        public const string CityCoreClassicCityUpdate = ContractPermissionKeys.CityCoreClassicCityUpdate;
        public const string CityCoreClassicCityArchive = ContractPermissionKeys.CityCoreClassicCityArchive;
        public const string CityCoreClassicCityDelete = ContractPermissionKeys.CityCoreClassicCityDelete;

        public const string CityCoreClassicCityPopulationBootstrapRetry =
            ContractPermissionKeys.CityCoreClassicCityPopulationBootstrapRetry;

        public const string CityCoreSimulationRead = ContractPermissionKeys.CityCoreSimulationRead;
        public const string CityCoreSimulationControl = ContractPermissionKeys.CityCoreSimulationControl;
    }
}
