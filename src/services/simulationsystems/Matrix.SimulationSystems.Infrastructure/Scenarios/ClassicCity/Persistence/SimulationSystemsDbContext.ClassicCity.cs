using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationSystems.Infrastructure.Persistence
{
    public partial class SimulationSystemsDbContext
    {
        public DbSet<CityEnvironmentalConditionState> CityEnvironmentalConditions
            => Set<CityEnvironmentalConditionState>();

        public DbSet<CitySystemsDeletionState> CitySystemsDeletionStates => Set<CitySystemsDeletionState>();
    }
}
