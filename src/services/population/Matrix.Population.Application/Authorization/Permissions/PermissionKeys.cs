using ContractPermissionKeys = Matrix.Population.Contracts.Authorization.Permissions.PermissionKeys;

namespace Matrix.Population.Application.Authorization.Permissions
{
    public static class PermissionKeys
    {
        public const string PopulationPeopleInitialize = ContractPermissionKeys.PopulationPeopleInitialize;
        public const string PopulationPeopleRead = ContractPermissionKeys.PopulationPeopleRead;

        public const string PopulationPersonResurrect = ContractPermissionKeys.PopulationPersonResurrect;
        public const string PopulationPersonKill = ContractPermissionKeys.PopulationPersonKill;
    }
}
