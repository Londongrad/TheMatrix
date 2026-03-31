using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CityOperationalBudgetPressureState
    {
        private CityOperationalBudgetPressureState() { }

        private CityOperationalBudgetPressureState(
            decimal balance,
            decimal municipalOperationsExpenses,
            decimal generalAvailableAmount,
            decimal operationsAvailableAmount,
            decimal infrastructureAvailableAmount,
            decimal healthcareAvailableAmount,
            string generalAuthorizationLevel,
            string operationsAuthorizationLevel,
            string infrastructureAuthorizationLevel,
            string healthcareAuthorizationLevel,
            decimal pressureIndex,
            long effectiveTickId,
            DateTimeOffset effectiveAtUtc)
        {
            Balance = balance;
            MunicipalOperationsExpenses = municipalOperationsExpenses;
            GeneralAvailableAmount = generalAvailableAmount;
            OperationsAvailableAmount = operationsAvailableAmount;
            InfrastructureAvailableAmount = infrastructureAvailableAmount;
            HealthcareAvailableAmount = healthcareAvailableAmount;
            GeneralAuthorizationLevel = generalAuthorizationLevel;
            OperationsAuthorizationLevel = operationsAuthorizationLevel;
            InfrastructureAuthorizationLevel = infrastructureAuthorizationLevel;
            HealthcareAuthorizationLevel = healthcareAuthorizationLevel;
            PressureIndex = pressureIndex;
            EffectiveTickId = effectiveTickId;
            EffectiveAtUtc = effectiveAtUtc;
        }

        public decimal Balance { get; private set; }
        public decimal MunicipalOperationsExpenses { get; private set; }
        public decimal GeneralAvailableAmount { get; private set; }
        public decimal OperationsAvailableAmount { get; private set; }
        public decimal InfrastructureAvailableAmount { get; private set; }
        public decimal HealthcareAvailableAmount { get; private set; }
        public string GeneralAuthorizationLevel { get; private set; } = string.Empty;
        public string OperationsAuthorizationLevel { get; private set; } = string.Empty;
        public string InfrastructureAuthorizationLevel { get; private set; } = string.Empty;
        public string HealthcareAuthorizationLevel { get; private set; } = string.Empty;
        public decimal PressureIndex { get; private set; }
        public long EffectiveTickId { get; private set; }
        public DateTimeOffset EffectiveAtUtc { get; private set; }

        public static CityOperationalBudgetPressureState Create(CityOperationalBudgetPressureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new CityOperationalBudgetPressureState(
                balance: snapshot.Balance,
                municipalOperationsExpenses: snapshot.MunicipalOperationsExpenses,
                generalAvailableAmount: snapshot.GeneralAvailableAmount,
                operationsAvailableAmount: snapshot.OperationsAvailableAmount,
                infrastructureAvailableAmount: snapshot.InfrastructureAvailableAmount,
                healthcareAvailableAmount: snapshot.HealthcareAvailableAmount,
                generalAuthorizationLevel: snapshot.GeneralAuthorizationLevel,
                operationsAuthorizationLevel: snapshot.OperationsAuthorizationLevel,
                infrastructureAuthorizationLevel: snapshot.InfrastructureAuthorizationLevel,
                healthcareAuthorizationLevel: snapshot.HealthcareAuthorizationLevel,
                pressureIndex: snapshot.PressureIndex,
                effectiveTickId: snapshot.EffectiveTickId,
                effectiveAtUtc: snapshot.EffectiveAtUtc);
        }

        public void ApplySnapshot(CityOperationalBudgetPressureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            Balance = snapshot.Balance;
            MunicipalOperationsExpenses = snapshot.MunicipalOperationsExpenses;
            GeneralAvailableAmount = snapshot.GeneralAvailableAmount;
            OperationsAvailableAmount = snapshot.OperationsAvailableAmount;
            InfrastructureAvailableAmount = snapshot.InfrastructureAvailableAmount;
            HealthcareAvailableAmount = snapshot.HealthcareAvailableAmount;
            GeneralAuthorizationLevel = snapshot.GeneralAuthorizationLevel;
            OperationsAuthorizationLevel = snapshot.OperationsAuthorizationLevel;
            InfrastructureAuthorizationLevel = snapshot.InfrastructureAuthorizationLevel;
            HealthcareAuthorizationLevel = snapshot.HealthcareAuthorizationLevel;
            PressureIndex = snapshot.PressureIndex;
            EffectiveTickId = snapshot.EffectiveTickId;
            EffectiveAtUtc = snapshot.EffectiveAtUtc;
        }

        public CityOperationalBudgetPressureSnapshot ToSnapshot()
        {
            return new CityOperationalBudgetPressureSnapshot(
                Balance: Balance,
                MunicipalOperationsExpenses: MunicipalOperationsExpenses,
                GeneralAvailableAmount: GeneralAvailableAmount,
                OperationsAvailableAmount: OperationsAvailableAmount,
                InfrastructureAvailableAmount: InfrastructureAvailableAmount,
                HealthcareAvailableAmount: HealthcareAvailableAmount,
                GeneralAuthorizationLevel: GeneralAuthorizationLevel,
                OperationsAuthorizationLevel: OperationsAuthorizationLevel,
                InfrastructureAuthorizationLevel: InfrastructureAuthorizationLevel,
                HealthcareAuthorizationLevel: HealthcareAuthorizationLevel,
                PressureIndex: PressureIndex,
                EffectiveTickId: EffectiveTickId,
                EffectiveAtUtc: EffectiveAtUtc);
        }
    }
}
