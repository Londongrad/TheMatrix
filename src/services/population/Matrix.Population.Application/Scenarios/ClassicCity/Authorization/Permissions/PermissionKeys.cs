using ContractPermissionKeys =
    Matrix.Population.Contracts.Scenarios.ClassicCity.Authorization.Permissions.PermissionKeys;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Authorization.Permissions
{
    public static class PermissionKeys
    {
        public const string PopulationPeopleInitialize = ContractPermissionKeys.PopulationPeopleInitialize;
        public const string PopulationCivilRegistryManage = ContractPermissionKeys.PopulationCivilRegistryManage;
        public const string PopulationEmploymentManage = ContractPermissionKeys.PopulationEmploymentManage;
    }
}
