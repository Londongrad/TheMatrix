using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CityOperationalBudgetPressureState
    {
        private CityOperationalBudgetPressureState() { }

        private CityOperationalBudgetPressureState(
            decimal balance,
            decimal municipalOperationsExpenses,
            decimal pressureIndex,
            DateTimeOffset effectiveAtUtc)
        {
            Balance = balance;
            MunicipalOperationsExpenses = municipalOperationsExpenses;
            PressureIndex = pressureIndex;
            EffectiveAtUtc = effectiveAtUtc;
        }

        public decimal Balance { get; private set; }
        public decimal MunicipalOperationsExpenses { get; private set; }
        public decimal PressureIndex { get; private set; }
        public DateTimeOffset EffectiveAtUtc { get; private set; }

        public static CityOperationalBudgetPressureState Create(CityOperationalBudgetPressureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new CityOperationalBudgetPressureState(
                balance: snapshot.Balance,
                municipalOperationsExpenses: snapshot.MunicipalOperationsExpenses,
                pressureIndex: snapshot.PressureIndex,
                effectiveAtUtc: snapshot.EffectiveAtUtc);
        }

        public void ApplySnapshot(CityOperationalBudgetPressureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            Balance = snapshot.Balance;
            MunicipalOperationsExpenses = snapshot.MunicipalOperationsExpenses;
            PressureIndex = snapshot.PressureIndex;
            EffectiveAtUtc = snapshot.EffectiveAtUtc;
        }

        public CityOperationalBudgetPressureSnapshot ToSnapshot()
        {
            return new CityOperationalBudgetPressureSnapshot(
                Balance: Balance,
                MunicipalOperationsExpenses: MunicipalOperationsExpenses,
                PressureIndex: PressureIndex,
                EffectiveAtUtc: EffectiveAtUtc);
        }
    }
}
