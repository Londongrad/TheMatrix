using Matrix.SimulationCore.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Migrations;

public sealed class AddSimulationInstancesMigrationTests
{
    [Fact]
    public void Up_ShouldCreateRegistryAndBackfillClassicCities()
    {
        IReadOnlyList<MigrationOperation> operations = new TestMigration().BuildUp();

        CreateTableOperation table = Assert.Single(
            operations.OfType<CreateTableOperation>(),
            operation => operation.Name == "SimulationInstances");
        SqlOperation backfill = Assert.Single(operations.OfType<SqlOperation>());

        Assert.Contains(table.Columns, column => column.Name == "Id");
        Assert.Contains(table.Columns, column => column.Name == "HostId");
        Assert.Contains(table.Columns, column => column.Name == "ScenarioKey");
        Assert.Contains(table.Columns, column => column.Name == "HostTypeKey");
        Assert.Contains("FROM \"Cities\"", backfill.Sql, StringComparison.Ordinal);
        Assert.Contains("'classic-city'", backfill.Sql, StringComparison.Ordinal);
        Assert.Contains("'city'", backfill.Sql, StringComparison.Ordinal);
        Assert.Contains("\"GenerationSeed\"", backfill.Sql, StringComparison.Ordinal);
        Assert.Contains("\"ScenarioModelSetVersion\"", backfill.Sql, StringComparison.Ordinal);
        Assert.Contains("\"Status\"", backfill.Sql, StringComparison.Ordinal);
    }

    private sealed class TestMigration : AddSimulationInstances
    {
        public IReadOnlyList<MigrationOperation> BuildUp()
        {
            var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

            Up(migrationBuilder);

            return migrationBuilder.Operations;
        }
    }
}
