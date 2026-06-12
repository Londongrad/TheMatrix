using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Resources.Infrastructure.Persistence
{
    public sealed partial class ResourcesDbContext
    {
        public DbSet<CityStockpileState> CityStockpiles => Set<CityStockpileState>();
        public DbSet<CityResourceDeletionState> CityResourceDeletionStates => Set<CityResourceDeletionState>();
    }
}
