using Matrix.SimulationCore.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Migrations;

public sealed class RekeySimulationClockToInstanceMigrationTests
{
    [Fact]
    public void Up_ShouldMoveClockForeignKeyFromCityToSimulationInstance()
    {
        IReadOnlyList<MigrationOperation> operations = new TestMigration().BuildUp();

        DropForeignKeyOperation droppedForeignKey = Assert.Single(operations.OfType<DropForeignKeyOperation>());
        AddForeignKeyOperation addedForeignKey = Assert.Single(operations.OfType<AddForeignKeyOperation>());

        Assert.Equal("FK_SimulationClocks_Cities_Id", droppedForeignKey.Name);
        Assert.Equal("SimulationClocks", addedForeignKey.Table);
        Assert.Equal("SimulationInstances", addedForeignKey.PrincipalTable);
        Assert.Equal("Id", Assert.Single(addedForeignKey.Columns));
        Assert.Equal(ReferentialAction.Cascade, addedForeignKey.OnDelete);
    }

    private sealed class TestMigration : RekeySimulationClockToInstance
    {
        public IReadOnlyList<MigrationOperation> BuildUp()
        {
            var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

            Up(migrationBuilder);

            return migrationBuilder.Operations;
        }
    }
}
